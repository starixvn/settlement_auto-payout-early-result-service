using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Exceptions;
using Fanex.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyConfirmScore(ResultDetailService resultDetailService, IOptions<AppSettingsKey> options)
    : ICondition
{
    public async Task HandleCondition(Match match)
    {
        var timeResult = match.ResultDetails.Where(x => x.ResultType == ResultTypes.Time);
        match.ResultDetails = match.ResultDetails.Where(x => x.ResultType != ResultTypes.Time).ToList();

        var scores = await resultDetailService.GetScores(match);
        CompareScore(match, scores);
        await LogService.InfoAsync($"AP2 GetScores {match.MatchId} Resources: {JsonConvert.SerializeObject(match.ResultDetails)}");

        var failureResults = match.ResultDetails.Where(r => !r.IsValid && r.Expired).ToList();
        var failureResourceIds = failureResults.Select(x => x.ResourceId).ToList();
        var errorMessage = string.Format(ErrorMessage.InconsistentScore, string.Join(",", failureResourceIds));

        await resultDetailService.UpdateResultStatus(match.MatchId, failureResults, errorMessage);

        match.ResultDetails = match.ResultDetails.Where(r => r.IsValid && r.ExternalSourceInfo.Any()).ToList();
        match.ResultDetails = [.. match.ResultDetails, .. timeResult];

        if (match.ResultDetails.Count == 0)
        {
            throw new ServiceException { MarkFail = false };
        }
    }

    private void CompareScore(Match match, IEnumerable<Score> scores)
    {
        var groupedScores = scores.GroupBy(s => new { s.ResourceId }).ToList();

        groupedScores.ForEach(group =>
        {
            var resultDetail = match.ResultDetails.First(r => r.ResourceId.Trim() == group.Key.ResourceId.Trim());
            if (group.Distinct(new ScoreComparer()).Count() == 1)
            {
                resultDetail.HomeValue = group.First().HomeValue;
                resultDetail.AwayValue = group.First().AwayValue;
                resultDetail.ExternalSourceInfo = group.Select(g => new ExternalSourceInfo
                { SourceName = g.SourceName, ResourceId = g.ResourceId, SourceId = g.SourceId });
            }
            else
            {
                MarkFail(resultDetail, group, match.MatchId);
            }
        });
    }

    private void MarkFail(ResultDetail resultDetail, IGrouping<object, Score> groupScore, int matchId)
    {
        var maxDate = groupScore.Max(t => t.CreateDate);
        var processTime = DateTimeOffset.Now - maxDate;
        if (processTime.TotalMilliseconds > options.Value.RetryProcessScore)
        {
            resultDetailService.WriteLogResultInvalid(matchId, resultDetail, groupScore.ToList());
            resultDetail.Expired = true;
        }

        resultDetail.IsValid = false;
    }
}