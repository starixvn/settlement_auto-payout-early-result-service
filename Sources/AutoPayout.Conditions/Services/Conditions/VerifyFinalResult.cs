using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;
using AutoPayout.Constants;
using Microsoft.IdentityModel.Tokens;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyFinalResult(ResultDetailService resultDetailService) : ICondition
{
    public async Task HandleCondition(Match match)
    {
        var finalResult = match.ResultDetails.Find(i => i.ResourceId == ResourceIds.FullTime);

        if (ShouldSkipFinalResultCheck(match, finalResult))
        {
            return;

        }

        if (await IsInvalidFinalResultForGameWinner(match, finalResult))
        {
            return;
        }

        var maxSetPlayed = int.Parse(finalResult.HomeValue) + int.Parse(finalResult.AwayValue);
        var settledResults = await resultDetailService.GetResultSettled(match.MatchId, match.SportType);

        var (setsResults, resultIndexes) = await resultDetailService.GetResultsChecking(match, maxSetPlayed, settledResults);

        var (minSet, maxSet) = VerifyFinalResultHelper.GetMinMaxSetPlay(match.MatchDetail);

        var isFinalConsistentWithGameSession = VerifyFinalResultHelper.IsValidFinalResultSetWin(setsResults, finalResult, minSet, maxSet);

        var isValid = !setsResults.IsNullOrEmpty()
            && isFinalConsistentWithGameSession
            && VerifyFinal(settledResults, setsResults, finalResult, maxSetPlayed);

        if (!isValid)
        {
            await HandleFinalResult(finalResult, match, !isFinalConsistentWithGameSession);
        }
        else
        {
            match.ResourceRefund = GetResourceToRefund(settledResults, resultIndexes, match.MatchDetail, maxSetPlayed);

        }

    }

    private static bool VerifyFinal(IEnumerable<ResultDetail> settledResults, IReadOnlyCollection<ResultDetail> setsResults, ResultDetail finalResult, int maxSetPlayed)
    {
        return VerifyFinalResultHelper.VerifyResultSettled(settledResults, maxSetPlayed)
            && VerifyFinalResultHelper.IsMatchResultsConsecutive(setsResults)
            && VerifyFinalResultHelper.VerifyResult(setsResults, finalResult);
    }

    private async Task HandleFinalResult(ResultDetail finalResult, Match match, bool isFinalInconsistentWithGameSession)
    {
        var message = isFinalInconsistentWithGameSession ? ErrorMessage.FinalInconsistentWithGameSession : ErrorMessage.FinalInconsistent;
        var errorMessage = message.Replace("@score", $"{finalResult.HomeValue}-{finalResult.AwayValue}");

        await resultDetailService.UpdateInvalidResultStatus(match.MatchId, new List<ResultDetail> { finalResult }, errorMessage);
        resultDetailService.WriteLogResult(match.MatchId, finalResult, errorMessage);
        match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != ResourceIds.FullTime).ToList();
    }


    private static string GetResourceToRefund(
        IEnumerable<ResultDetail> resultSettled,
        IReadOnlyList<ResourceIndex> resourceIndexes,
        MatchDetail matchDetail,
        int maxGamePlayed)
    {
        var resourceToRefund = new List<string>();
        var resourceSettled = resultSettled.Select(x => x.ResourceId).ToList();

        for (var i = maxGamePlayed + 1; i <= matchDetail.GameSession; i++)
        {
            var resource = resourceIndexes.FirstOrDefault(r => r.IntervalNo == i);
            if (resource is not null && !resourceSettled.Contains(resource.ResourceId))
            {
                resourceToRefund.Add(resource.ResourceId);
            }
        }

        return string.Join(",", resourceToRefund);
    }

    private bool ShouldSkipFinalResultCheck(Match match, ResultDetail finalResult)
    {
        return match.ResourceType != ResourceType.MainResource
               || finalResult is null
               || (match.SportType == SportType.BaseBall && (int)match.MatchDetail.SessionTime == 0);
    }

    private async Task<bool> IsInvalidFinalResultForGameWinner(Match match, ResultDetail finalResult)
    {
        if (!IsChildMatch(match))
        {
            return false;
        }

        if (IsGameWinnerType(match) && IsZeroScore(finalResult))
        {
            await HandleInvalidResult(match, finalResult, ErrorMessage.InvalidScoreGameWinner);
            return true;
        }

        return true;
    }

    private async Task HandleInvalidResult(Match match, ResultDetail finalResult, string errorMessage)
    {
        await resultDetailService.UpdateInvalidResultStatus(match.MatchId, new List<ResultDetail> { finalResult }, errorMessage);
        resultDetailService.WriteLogResult(match.MatchId, finalResult, errorMessage);
        match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != ResourceIds.FullTime).ToList();
    }


    private bool IsChildMatch(Match match) => match.MatchDetail.ParentID != 0;

    private bool IsGameWinnerType(Match match)
    {
        int[] gameWinnerTypes = { 12, 14, 15, 16, 17 };
        return match.SportType == SportType.Tennis
            && gameWinnerTypes.Contains(match.MatchDetail.ChildMatchType);
    }

    private bool IsZeroScore(ResultDetail finalResult) => finalResult.HomeValue == "0" && finalResult.AwayValue == "0";
}