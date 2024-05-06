using AutoPayout.Constants;
using AutoPayout.Conditions.Services.HelperSerivce;
using AutoPayout.Exceptions;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.Services;

public class VerifyConfirmScore(ResultDetailService resultDetailService, IOptions<AppSettingsKey> options)
    : ICondition
{
    public async Task HandleCondition(Match match)
    {
        var timeResult = match.ResultDetails.Where(x => x.ResultType == ResultTypes.Time);
        match.ResultDetails = match.ResultDetails.Where(x => x.ResultType != ResultTypes.Time);

        var scores = await resultDetailService.GetScore(match);

        CompareScore(match, scores);

        await UpdateInvalidResultStatus(match);


        match.ResultDetails = match.ResultDetails.Where(r => r.IsValid && r.ExternalSourceInfo.Any());
        match.ResultDetails = match.ResultDetails.Concat(timeResult);

        if (!match.ResultDetails.Any())
        {
            throw new ServiceException { MarkFail = false };
        }
    }

    private async Task UpdateInvalidResultStatus(Match match)
    {
        var failureResults = match.ResultDetails.Where(r => !r.IsValid && r.Expired);
        var failureResourceIds = failureResults.Select(x => x.ResourceId).ToList();
        var errorMessage = string.Format(ErrorMessage.InconsistentScore, string.Join(",", failureResourceIds));

        await resultDetailService.UpdateResultStatus(match.MatchId, failureResults, errorMessage);
    }

    private void CompareScore(Match match, IEnumerable<Score> scores)
    {
        var groupedScores = scores.GroupBy(s => new KeyGroupScore(s.ResourceId, s.ResultType)).ToList();

        foreach (var group in groupedScores)
        {
            var resultDetail = match.ResultDetails
                .First(r => r.ResourceId.Trim() == group.Key.ResourceId.Trim() && r.ResultType == group.Key.ResultType);

            if (!IsTimeResult(group) && group.Distinct(new ScoreComparer()).Count() == 1)
            {
                SetResultDetail(resultDetail, group);
                break;
            }

            if (IsTimeResult(group) && HasTraderScore(group) && group.Distinct(new ScoreComparer(true)).Count() == 1)
            {
                SetTraderForResultDetail(resultDetail, group);
                break;
            }

            MarkFail(resultDetail, group, match.MatchId);
        }
    }

    private bool IsTimeResult(IGrouping<KeyGroupScore, Score> group) => group.Key.ResultType == ResultTypes.Time;

    private bool HasTraderScore(IGrouping<KeyGroupScore, Score> group)
    {
        var traderScore = group.FirstOrDefault(x => x.SourceId == SourceId.Trader);

        var result = traderScore is not null
            && !string.IsNullOrEmpty(traderScore.AwayValue)
            && !string.IsNullOrEmpty(traderScore.HomeValue);

        return result;
    }

    private void SetTraderForResultDetail(ResultDetail resultDetail, IGrouping<KeyGroupScore, Score> group)
    {
        var traderScore = group.First(x => x.SourceId == SourceId.Trader);
        resultDetail.HomeValue = traderScore.HomeValue;
        resultDetail.AwayValue = traderScore.AwayValue;

        var exInfo = group.Select(g => new ExternalSourceInfo
        {
            SourceName = g.SourceName,
            ResourceId = g.ResourceId,
            SourceId = g.SourceId
        });
        resultDetail.ExternalSourceInfo = exInfo;
    }

    private void SetResultDetail(ResultDetail resultDetail, IGrouping<KeyGroupScore, Score> groupScore)
    {
        var firstScore = groupScore.First();
        resultDetail.HomeValue = firstScore.HomeValue;
        resultDetail.AwayValue = firstScore.AwayValue;

        var exInfo = groupScore.Select(g => new ExternalSourceInfo
        {
            SourceName = g.SourceName,
            ResourceId = g.ResourceId,
            SourceId = g.SourceId
        });
        resultDetail.ExternalSourceInfo = exInfo;
    }

    private void MarkFail(ResultDetail resultDetail, IGrouping<KeyGroupScore, Score> groupScore, int matchId)
    {
        var maxDate = groupScore.Max(t => t.CreateDate);
        var processTime = DateTimeOffset.Now - maxDate;

        if (IsTimeResult(groupScore) && !HasTraderScore(groupScore) && processTime.TotalMinutes > 30) // no trader
        {
            resultDetailService.WriteLogResultNotTrader(matchId, resultDetail, groupScore.ToList());
            resultDetail.Expired = true;
        }
        else if (processTime.TotalMilliseconds > options.Value.RetryProcessScore)
        {
            resultDetailService.WriteLogResultInvalid(matchId, resultDetail, groupScore.ToList());
            resultDetail.Expired = true;
        }

        resultDetail.IsValid = false;
    }
}