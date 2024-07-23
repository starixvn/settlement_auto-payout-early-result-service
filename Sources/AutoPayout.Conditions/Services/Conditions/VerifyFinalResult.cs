using AutoPayout.Conditions.Services.HelperSerivce.VerifyFinalResult;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using Microsoft.IdentityModel.Tokens;

namespace AutoPayout.Conditions.Services;

public class VerifyFinalResult(IDynamicRepository repository, ResultDetailService resultDetailService) : ICondition
{
    public async Task HandleCondition(Match match)
    {
        var finalResult = match.ResultDetails.Find(i => i.ResourceId == ResourceIds.FullTime);
        if (match.ResourceType != ResourceType.MainResource || finalResult is null)
        {
            return;
        }

        var maxSetPlayed = int.Parse(finalResult.HomeValue) + int.Parse(finalResult.AwayValue);
        var settledResults = await resultDetailService.GetResultSettled(match.MatchId, match.SportType);

        var (setsResults, _) = await resultDetailService.GetResultsChecking(match, maxSetPlayed, settledResults);

        var matchDetail = await repository.GetAsync<MatchDetail>(new GetMatchDetailCriteria { MatchId = match.MatchId });

        var (minSet, maxSet) = VerifyFinalResultHelper.GetMinMaxSetPlay(matchDetail);

        var isFinalConsistentWithGameSession = VerifyFinalResultHelper.IsValidFinalResult(setsResults, finalResult, minSet, maxSet);

        var isValid = !setsResults.IsNullOrEmpty()
            && isFinalConsistentWithGameSession
            && VerifyFinal(settledResults, setsResults, finalResult, maxSetPlayed);

        if (!isValid)
        {
            await HandleFinalResult(finalResult, match, !isFinalConsistentWithGameSession);
        }
    }

    private bool VerifyFinal(List<ResultDetail> settledResults, IReadOnlyCollection<ResultDetail> setsResults, ResultDetail finalResult, int maxSetPlayed)
    {
        return VerifyFinalResultHelper.VerifyResultSettled(settledResults, maxSetPlayed)
            && VerifyFinalResultHelper.IsMatchResultsConsecutive(setsResults)
            && VerifyFinalResultHelper.VerifyResult(setsResults, finalResult);
    }

    private async Task HandleFinalResult(ResultDetail finalResult, Match match, bool isFinalInconsistentWithGameSession) //NOSONAR
    {
        var message = isFinalInconsistentWithGameSession ? ErrorMessage.FinalInconsistentWithGameSession : ErrorMessage.FinalInconsistent;
        var errorMessage = message.Replace("@score", $"{finalResult.HomeValue}-{finalResult.AwayValue}");

        await resultDetailService.UpdateResultStatus(match.MatchId, new List<ResultDetail> { finalResult }, errorMessage);
        resultDetailService.WriteLogResult(match.MatchId, finalResult, errorMessage);
        match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != ResourceIds.FullTime).ToList();
    }
}
