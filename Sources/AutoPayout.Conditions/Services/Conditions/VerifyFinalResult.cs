using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;
using AutoPayout.Constants;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyFinalResult(ResultDetailService resultDetailService, IOptions<AppSettingsKey> appSettingOptions) : AbstractCondition
{
    public override async Task HandleCondition(Match match)
    {
        match.LogCondition.Add($"{ConditionId.VerifyFinalResultGameSession} - {GetType().Name}");
        var finalResult = match.ResultDetails.Find(i => i.ResourceId == ResourceIds.FullTime);

        if (ShouldSkipFinalResultCheck(match, finalResult) || await IsInvalidFinalResultForGameWinner(match, finalResult))
        {
            await HandleNextCondition(match);
            return;
        }

        var (minSet, maxSet) = VerifyFinalResultHelper.GetMinMaxSetPlay(match.MatchDetail);

        if (!VerifyFinalResultHelper.IsValidFinalResultSetWin(finalResult, minSet, maxSet))
        {
            await HandleFinalResult(finalResult, match, true);
            await HandleNextCondition(match);
            return;
        }

        var maxSetPlayed = GetMaxSetPlayed(finalResult);
        var resourceIndexes = await resultDetailService.GetResourceIndex(match.SportType);
        var validResourcesPlayed = resourceIndexes
            .Where(x => x.IntervalNo <= maxSetPlayed && x.ResourceId != ResourceIds.ExtraInning) // ignore baseball
            .ToList();

        var (isSetSettled, settledResults) = await CheckSetSettled(match, validResourcesPlayed, finalResult);

        if (!isSetSettled)
        {
            await HandleNextCondition(match);
            return;
        }

        var setsResults = resultDetailService.GetValidSetResults(match, settledResults, resourceIndexes, maxSetPlayed);

        var isValid = setsResults.Count != 0
            && VerifyValidRunningSetResults(match.ResultDetails, resourceIndexes, maxSetPlayed)
            && VerifyFinal(settledResults, setsResults, finalResult, maxSetPlayed);

        if (!isValid)
        {
            await HandleFinalResult(finalResult, match, false);
        }
        else
        {
            match.ResourceRefund = GetResourceToRefund(settledResults, resourceIndexes, maxSet, maxSetPlayed);
        }

        await HandleNextCondition(match);
    }


    private bool VerifyValidRunningSetResults(List<ResultDetail> resultDetails, IEnumerable<ResourceIndex> resourceIndexes, int maxSetPlayed)
    {
        var inValidResources = resourceIndexes.Where(x => x.IntervalNo > maxSetPlayed)
            .Select(x => x.ResourceId);

        return !resultDetails.Where(x => x.ResourceId != ResourceIds.FullTime)
            .Any(x => inValidResources.Contains(x.ResourceId));
    }

    private async Task<(bool, List<ResultDetail>)> CheckSetSettled(Match match, IEnumerable<ResourceIndex> validResourcesPlayed, ResultDetail finalResult)
    {
        var settledResults = await resultDetailService.GetResultSettled(match.MatchId, match.SportType);
        var setSettlingResults = match.ResultDetails
            .Where(x => x.ResourceId != ResourceIds.FullTime)
            .Select(x => x.ResourceId).ToHashSet();

        var maxTimeScore = await resultDetailService.MaxTimeScore(finalResult);

        var existsSettle = validResourcesPlayed.All(resource => setSettlingResults.TryGetValue(resource.ResourceId, out var _)
                || settledResults.Exists(x => x.ResourceId == resource.ResourceId));

        if (existsSettle)
        {
            return (true, settledResults);
        }

        if (maxTimeScore > appSettingOptions.Value.RetryProcessScore) // false greater 5 min
        {
            var resultRelatedValid = settledResults.Select(x => x.ResourceId)
                .Intersect(validResourcesPlayed.Select(x => x.ResourceId))
                .ToList();

            var errorMsg = CreateMessageService.RelatedResultInvalid(finalResult, resultRelatedValid, validResourcesPlayed.Select(x => x.ResourceId), false);
            await HandleInvalidResult(match, finalResult, errorMsg);

            return (false, settledResults);
        }

        match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != ResourceIds.FullTime).ToList();
        return (false, settledResults);

    }
    private static bool VerifyFinal(
        IEnumerable<ResultDetail> settledResults,
        IReadOnlyCollection<ResultDetail> setsResults,
        ResultDetail finalResult,
        int maxSetPlayed)
            => VerifyFinalResultHelper.VerifyFakeResultSettled(settledResults, maxSetPlayed)
               && VerifyFinalResultHelper.IsMatchResultsConsecutive(setsResults)
               && VerifyFinalResultHelper.VerifyScoreSetWithFinalResult(setsResults, finalResult);

    private async Task HandleFinalResult(ResultDetail finalResult, Match match, bool isFinalInconsistentWithGameSession)
    {
        var message = isFinalInconsistentWithGameSession ? ErrorMessage.FinalInconsistentWithGameSession : ErrorMessage.FinalInconsistent;
        var errorMessage = message.Replace("@score", $"{finalResult.HomeValue}-{finalResult.AwayValue}");

        await resultDetailService.UpdateInvalidResultStatus(match.MatchId, new List<ResultDetail> { finalResult }, errorMessage);
        resultDetailService.WriteLogResult(match.MatchId, finalResult, errorMessage);
        match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != ResourceIds.FullTime).ToList();
    }


    private static string GetResourceToRefund(IEnumerable<ResultDetail> resultSettled, IEnumerable<ResourceIndex> resourceIndexes, int maxSet, int maxGamePlayed) =>
        string.Join(",", resourceIndexes
            .Where(r => r.IntervalNo > maxGamePlayed && r.IntervalNo <= maxSet)
            .Select(r => r.ResourceId)
            .Except(resultSettled.Select(r => r.ResourceId)));

    private bool ShouldSkipFinalResultCheck(Match match, ResultDetail finalResult) => match.ResourceType != ResourceType.MainResource
               || finalResult is null
               || (match.SportType == SportType.BaseBall && (int)match.MatchDetail.SessionTime == 0);

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

        match.ResultDetails.RemoveAll(r => r.ResourceId == ResourceIds.FullTime);
    }

    private static int GetMaxSetPlayed(ResultDetail finalResult)
        => int.TryParse(finalResult.HomeValue, out var homeScore) && int.TryParse(finalResult.AwayValue, out var awayScore)
        ? homeScore + awayScore
        : 0;

    private static bool IsChildMatch(Match match) => match.MatchDetail.ParentID != 0;

    private static bool IsGameWinnerType(Match match) =>
           match.SportType is SportType.Tennis or SportType.BeachVolleyball && new[] { 12, 14, 15, 16, 17 }.Contains(match.MatchDetail.ChildMatchType);

    private static bool IsZeroScore(ResultDetail finalResult) => finalResult.HomeValue == "0" && finalResult.AwayValue == "0";

}