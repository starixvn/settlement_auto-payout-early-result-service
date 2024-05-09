using AutoPayout.Conditions.Services.HelperSerivce.VerifyFinalResult;
using AutoPayout.Constants;
using Microsoft.IdentityModel.Tokens;

namespace AutoPayout.Conditions.Services;

public class VerifyFinalResult(IDynamicRepository repository, ResultDetailService resultDetailService) : ICondition
{
    public async Task HandleCondition(Match match)
    {
        var finalResult = match.ResultDetails.FirstOrDefault(i => i.ResourceId == ResourceId.Final);
        if (match.ResourceType != ResourceType.MainResource || finalResult is null)
        {
            return;
        }

        var maxSetPlayed = int.Parse(finalResult.HomeValue) + int.Parse(finalResult.AwayValue);
        var settledResults = await resultDetailService.GetResultSettled(match.MatchId, match.SportType);

        var (setsResults, _) = await resultDetailService.GetResultsChecking(match, maxSetPlayed, settledResults);

        var matchDetail = await repository.GetAsync<MatchDetail>(new GetMatchDetailCriteria { MatchId = match.MatchId });

        var (minSet, _) = VerifyFinalResultHelper.GetMinMaxSetPlay(matchDetail, true);

        var isFinalConsistentWithGameSession = VerifyFinalResultHelper.IsValidFinalResult(setsResults, finalResult, minSet);

        var isValid = !setsResults.IsNullOrEmpty()
            && isFinalConsistentWithGameSession
            && VerifyFinalResultHelper.VerifyResultSettled(settledResults, maxSetPlayed)
            && VerifyFinalResultHelper.IsMatchResultsConsecutive(setsResults)
            && VerifyFinalResultHelper.VerifyResult(setsResults, finalResult);

        if (!isValid)
        {
            await HandleFinalResult(finalResult, match, !isFinalConsistentWithGameSession);
        }
    }

    private async Task HandleFinalResult(ResultDetail finalResult, Match match, bool isFinalInconsistentWithGameSession) //NOSONAR
    {
        var message = isFinalInconsistentWithGameSession ? ErrorMessage.FinalInconsistentWithGameSession : ErrorMessage.FinalInconsistent;
        var errorMessage = message.Replace("@score", $"{finalResult.HomeValue}-{finalResult.AwayValue}");

        await resultDetailService.UpdateResultStatus(match.MatchId, new List<ResultDetail> { finalResult }, errorMessage);
        resultDetailService.WriteLogResult(match.MatchId, finalResult, errorMessage);
        match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != ResourceId.Final);
    }
}