using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Exceptions;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyTimeResult : ICondition
{
    private readonly ResultDetailService resultDetailService;
    private readonly AppSettingsKey appSettingsKey;

    public VerifyTimeResult(ResultDetailService resultDetailService, IOptions<AppSettingsKey> options)
    {
        this.resultDetailService = resultDetailService;
        appSettingsKey = options.Value;
    }

    public async Task HandleCondition(Match match)
    {
        var timeResults = match.ResultDetails.Where(x => x.ResultType == ResultTypes.Time);

        if (!timeResults.Any())
        {
            return;
        }

        var normalResults = match.ResultDetails.Where(x => x.ResultType != ResultTypes.Time);
        match.ResultDetails = match.ResultDetails.Where(x => x.ResultType == ResultTypes.Time).ToList();

        var scores = await resultDetailService.GetScores(match);

        CompareScore(match, scores);

        match.ResultDetails = match.ResultDetails.Where(r => r.IsValid && r.ExternalSourceInfo.Any()).ToList();
        match.ResultDetails = match.ResultDetails.Concat(normalResults).ToList();

        if (!match.ResultDetails.Any())
        {
            throw new ServiceException { MarkFail = false };
        }
    }

    private void CompareScore(Match match, IEnumerable<Score> scores)
    {
        var groupedScores = scores.GroupBy(s => new KeyGroupScore(s.ResourceId, s.ResultType)).ToList();

        groupedScores.ForEach(async group =>
        {
            var resultDetail = match.ResultDetails.First(r => r.ResourceId.Trim() == group.Key.ResourceId.Trim());

            if (TryGetTraderScore(group, out string minute, out string second)
                && group.Distinct(new ScoreComparer(true)).Count() == 1
                && Convert.ToInt32(second) >= 0 && Convert.ToInt32(second) < 60)
            {
                resultDetail.HomeValue = minute;
                resultDetail.AwayValue = second;

                resultDetail.ExternalSourceInfo = group.Select(g => new ExternalSourceInfo
                {
                    SourceName = g.SourceName,
                    ResourceId = g.ResourceId,
                    SourceId = g.SourceId
                });
            }
            else
            {
                await MarkFail(resultDetail, group, match);
            }
        });
    }

    private bool TryGetTraderScore(IGrouping<KeyGroupScore, Score> group, out string homeScore, out string awayScore)
    {
        var traderScore = group.FirstOrDefault(x => x.SourceId == SourceId.Trader);

        var result = traderScore is not null
            && !string.IsNullOrEmpty(traderScore.AwayValue)
            && !string.IsNullOrEmpty(traderScore.HomeValue);

        homeScore = traderScore?.HomeValue;
        awayScore = traderScore?.AwayValue;

        return result;
    }

    private async Task MarkFail(ResultDetail resultDetail, IGrouping<KeyGroupScore, Score> groupScore, Match match)
    {
        var maxDate = groupScore.Max(t => t.CreateDate);
        var processTime = DateTimeOffset.Now - maxDate;

        if (!TryGetTraderScore(groupScore, out string _, out string _) && processTime.TotalSeconds > match.EarlyRetryTime)
        {
            var remark = resultDetailService.WriteLogResultNotTrader(match.MatchId, resultDetail);
            await resultDetailService.UpdateResultStatus(match.MatchId, new List<ResultDetail>() { resultDetail }, remark);
            resultDetail.Expired = true;
        }
        else if (TryGetTraderScore(groupScore, out string _, out string _) && processTime.TotalMilliseconds > appSettingsKey.RetryProcessScore)
        {
            var remark = resultDetailService.WriteLogResultInvalid(match.MatchId, resultDetail, groupScore.ToList());
            await resultDetailService.UpdateResultStatus(match.MatchId, new List<ResultDetail>() { resultDetail }, remark);
            resultDetail.Expired = true;
        }

        resultDetail.IsValid = false;
    }
}