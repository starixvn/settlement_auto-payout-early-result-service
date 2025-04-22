using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;
using AutoPayout.Constants;
using Microsoft.IdentityModel.Tokens;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyFinalByBestOfMap(ResultDetailService resultDetailService) : AbstractCondition
{
    public override async Task HandleCondition(Match match)
    {
        match.LogCondition.Add($"{ConditionId.VerifyFinalResultByBestOfMap} - {GetType().Name}");
        var finalResult = match.ResultDetails.Find(i => i.ResourceId == ResourceIds.FullTime);
        if (match.ResourceType != ResourceType.MainResource || finalResult is null || match.MatchDetail.ParentID != 0)
        {
            return;
        }

        var matchDetail = match.MatchDetail;

        var settledResults = await resultDetailService.GetResultSettled(match.MatchId, match.SportType);

        var maxGamePlayed = GetMaxGamePlayed(matchDetail, finalResult);

        var (resultsChecking, resultIndexes) = await resultDetailService.GetResultsChecking(match, maxGamePlayed, settledResults);

        var isValid = ValidateResult(resultsChecking, matchDetail, match, settledResults, maxGamePlayed);

        if (isValid)
        {
            match.ResourceRefund = GetResourceToRefund(settledResults, resultIndexes, matchDetail, maxGamePlayed);
        }
        else
        {
            await HandleFinalResult(finalResult, match);
        }

        await HandleNextCondition(match);
    }

    private static int GetMaxGamePlayed(MatchDetail matchDetail, ResultDetail finalResult)
    {
        if (matchDetail.OverTimeSession == OverTimeSessions.OverWatch && matchDetail.BestOfMap % GameType.Bo2 == 0)
        {
            return matchDetail.BestOfMap;
        }

        return int.Parse(finalResult.HomeValue) + int.Parse(finalResult.AwayValue);
    }

    private static string GetResourceToRefund(IEnumerable<ResultDetail> resultSettled, IReadOnlyList<ResourceIndex> resourceIndexes, MatchDetail matchDetail, int maxGamePlayed)
    {
        if (matchDetail.BestOfMap is GameType.Bo1 or GameType.Bo2)

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

    private static bool ValidateResult(List<ResultDetail> results, MatchDetail matchDetail, Match match, List<ResultDetail> settledResults, int maxGamePlayed)
    {
        if (results.IsNullOrEmpty())
        {
            return false;
        }

        var finalResult = match.ResultDetails.Find(i => i.ResourceId == ResourceIds.FullTime);
        if (matchDetail.BestOfMap == GameType.Bo1)
        {
            return VerifyFinalResultHelper.VerifyFinalBO1(finalResult, results);
        }

        if (matchDetail.BestOfMap == GameType.Bo2)
        {
            return ValidateBestOfTwo(finalResult, results);
        }

        if (IsOverTimeSession(matchDetail))
        {
            return ValidateOverTimeSession(results, finalResult, maxGamePlayed);
        }

        return ValidateGeneralCase(match, results, matchDetail, settledResults, finalResult, maxGamePlayed);
    }

    private static bool ValidateBestOfTwo(ResultDetail finalResult, List<ResultDetail> results)
    {
        return VerifyFinalResultHelper.VerifyFinalBestOfTwo(finalResult, results) &&
               VerifyFinalResultHelper.VerifyScoreSetWithFinalResult(results, finalResult);
    }

    private static bool IsOverTimeSession(MatchDetail matchDetail)
    {
        return matchDetail.OverTimeSession == OverTimeSessions.OverWatch && matchDetail.BestOfMap % GameType.Bo2 == 0;
    }

    private static bool ValidateOverTimeSession(IReadOnlyCollection<ResultDetail> results, ResultDetail finalResult, int maxGamePlayed)
    {
        return VerifyFinalResultHelper.VerifyDrawSetResults(results, maxGamePlayed) &&
               VerifyFinalResultHelper.IsMatchResultsConsecutive(results) &&
               VerifyFinalResultHelper.VerifyScoreSetWithFinalResult(results, finalResult);
    }

    private static bool ValidateGeneralCase(
        Match match,
        List<ResultDetail> results,
        MatchDetail matchDetail,
        IReadOnlyCollection<ResultDetail> settledResults,
        ResultDetail finalResult,
        int maxGamePlayed)
    {
        var (minSet, maxSet) = VerifyFinalResultHelper.GetMinMaxSetPlay(matchDetail, false);

        var verifyRelatedResult = VerifyFinalResultHelper.VerifyFakeResultSettled(settledResults, maxGamePlayed) &&
                                  VerifyFinalResultHelper.IsMatchResultsConsecutive(results) &&
                                  VerifyFinalResultHelper.VerifyScoreSetWithFinalResult(results, finalResult);

        return verifyRelatedResult &&
               VerifyFinalResultHelper.IsValidFinalResult(results, finalResult, minSet, maxSet) &&
               VerifyFinalResultHelper.VerifyAnyDrawResults([.. match.ResultDetails, .. settledResults]);
    }

    private async Task HandleFinalResult(ResultDetail finalResult, Match match)
    {
        var errorMessage = ErrorMessage.FinalESportsInconsistent.Replace("@score", $"{finalResult.HomeValue}-{finalResult.AwayValue}");

        await resultDetailService.UpdateInvalidResultStatus(match.MatchId, new List<ResultDetail> { finalResult }, errorMessage);

        resultDetailService.WriteLogResult(match.MatchId, finalResult, errorMessage);

        match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != ResourceIds.FullTime).ToList();
    }
}