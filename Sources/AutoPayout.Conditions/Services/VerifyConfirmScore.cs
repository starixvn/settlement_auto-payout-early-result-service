using AutoPayout.Exceptions;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.Services;

public class VerifyConfirmScore(ResultDetailService resultDetailService, IOptions<AppSettingsKey> options)
    : ICondition
{
    public async Task HandleCondition(Match match)
    {
        var scores = await resultDetailService.GetScore(match);
        CompareScore(match, scores);

        await resultDetailService.UpdateResultStatus(match.MatchId, match.ResultDetails.Where(r => !r.IsValid && r.Expired));
        match.ResultDetails = match.ResultDetails.Where(r => r.IsValid && r.ExternalSourceInfo.Any());

        if (!match.ResultDetails.Any())
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