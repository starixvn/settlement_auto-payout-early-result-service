using AutoPayout.Conditions.Services.HelperSerivce.VerifyFinalResult;
using AutoPayout.Constants;
using Microsoft.IdentityModel.Tokens;

namespace AutoPayout.Conditions.Services;

public class VerifyFinalByBestOfMap(IDynamicRepository repository, ResultDetailService resultDetailService) : ICondition
{
    private const int Bo1 = 1;
    private const int Bo2 = 2;

    public async Task HandleCondition(Match match)
    {
        var finalResult = match.ResultDetails.FirstOrDefault(i => i.ResourceId == ResourceId.Final);
        if (match.ResourceType != ResourceType.MainResource || finalResult is null)
        {
            return;
        }

        var matchDetail = await repository.GetAsync<MatchDetail>(new GetMatchDetailCriteria { MatchId = match.MatchId });
        var settledResults = await resultDetailService.GetResultSettled(match.MatchId, match.SportType);

        var maxGamePlayed = GetMaxGamePlayed(matchDetail, finalResult);

        var (resultsChecking, resultIndexes) = await resultDetailService.GetResultsChecking(match, maxGamePlayed, settledResults);

        var isValid = ValidateResult(resultsChecking, matchDetail, finalResult, settledResults, maxGamePlayed);

        if (isValid)
        {
            match.ResourceRefund = GetResourceToRefund(settledResults, resultIndexes, matchDetail, maxGamePlayed);
        }
        else
        {
            await HandleFinalResult(finalResult, match);
        }
    }

    private int GetMaxGamePlayed(MatchDetail matchDetail, ResultDetail finalResult)
    {
        if (matchDetail.OverTimeSession == OverTimeSessions.OverWatch && matchDetail.BestOfMap % 2 == 0)
        {
            return matchDetail.BestOfMap;
        }

        return int.Parse(finalResult.HomeValue) + int.Parse(finalResult.AwayValue);
    }

    private string GetResourceToRefund(IReadOnlyList<ResultDetail> resultSettled, IReadOnlyList<ResourceIndex> resourceIndexes, MatchDetail matchDetail, int maxGamePlayed)
    {
        if (matchDetail.BestOfMap is Bo1 or Bo2)

        {
            return string.Empty;
        }

        var resourceToRefund = new List<string>();
        var resourceSettled = resultSettled.Select(x => x.ResourceId).ToList();

        for (var i = maxGamePlayed + 1; i <= matchDetail.BestOfMap; i++)
        {
            var resource = resourceIndexes.FirstOrDefault(r => r.IntervalNo == i);
            if (resource is not null && !resourceSettled.Contains(resource.ResourceId))
            {
                resourceToRefund.Add(resource.ResourceId);
            }
        }

        return string.Join(",", resourceToRefund);
    }

    private bool ValidateResult(List<ResultDetail> results, MatchDetail matchDetail, ResultDetail finalResult, List<ResultDetail> settledResults, int maxGamePlayed)
    {
        if (results.IsNullOrEmpty()) return false;

        if (matchDetail.BestOfMap == Bo1)
        {
            return VerifyFinalResultHelper.VerifyFinalBO1(finalResult, results);
        }

        if (matchDetail.BestOfMap == Bo2)
        {
            return VerifyFinalResultHelper.VerifyFinalBestOfTWO(finalResult, results) &&
                     VerifyFinalResultHelper.VerifyResult(results, finalResult);
        }

        if (matchDetail.OverTimeSession == OverTimeSessions.OverWatch && matchDetail.BestOfMap % 2 == 0)
        {
            var temp = VerifyFinalResultHelper.VerifyDrawSetResults(results, maxGamePlayed);
            var temp1 = VerifyFinalResultHelper.IsMatchResultsConsecutive(results);
            var temp2 = VerifyFinalResultHelper.VerifyResult(results, finalResult);

            return temp && temp1 && temp2;
        }

        var (minSet, _) = VerifyFinalResultHelper.GetMinMaxSetPlay(matchDetail, false);

        return VerifyFinalResultHelper.VerifyResultSettled(settledResults, maxGamePlayed)
                    && VerifyFinalResultHelper.IsValidFinalResult(results, finalResult, minSet)
                    && VerifyFinalResultHelper.IsMatchResultsConsecutive(results)
                    && VerifyFinalResultHelper.VerifyResult(results, finalResult)
                    && VerifyFinalResultHelper.VerifyAnyDrawResults(results);
    }

    private async Task HandleFinalResult(ResultDetail finalResult, Match match)
    {
        var errorMessage = ErrorMessage.FinalESportsInconsistent.Replace("@score", $"{finalResult.HomeValue}-{finalResult.AwayValue}");

        await resultDetailService.UpdateResultStatus(match.MatchId, new List<ResultDetail> { finalResult }, errorMessage);

        resultDetailService.WriteLogResult(match.MatchId, finalResult, errorMessage);

        match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != ResourceId.Final);
    }
}