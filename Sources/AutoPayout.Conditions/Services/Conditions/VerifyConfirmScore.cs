using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyConfirmScore(
    ResultDetailService resultDetailService,
    ScoreMessageService scoreMessageService,
    IOptions<AppSettingsKey> options)
    : AbstractCondition
{
    public override async Task HandleCondition(Match match)
    {
        match.LogCondition.Add($"{ConditionId.VerifyConfirmScore} - {GetType().Name}");
        var timeResult = match.ResultDetails.Where(x => x.ResultType == ResultTypes.Time);
        match.ResultDetails = match.ResultDetails.Where(x => x.ResultType != ResultTypes.Time).ToList();

        var scores = await resultDetailService.GetScores(match);

        CompareScore(match, scores);

        await ProcessFailureResults(match, scores);

        match.ResultDetails = match.ResultDetails.Where(r => r.IsValid && r.ExternalSourceInfo.Any()).ToList();
        match.ResultDetails = [.. match.ResultDetails, .. timeResult];

        await HandleNextCondition(match);
    }

    private async Task ProcessFailureResults(Match match, IEnumerable<Score> scores)
    {
        var failureResults = match.ResultDetails.Where(r => !r.IsValid && r.Expired).ToList();
        var resourceIds = failureResults.Select(x => x.ResourceId).ToList();
        var remark = scoreMessageService.GetScoreMessage(scores, string.Join(",", resourceIds));

        await resultDetailService.UpdateInvalidResultStatus(match.MatchId, failureResults, remark);
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