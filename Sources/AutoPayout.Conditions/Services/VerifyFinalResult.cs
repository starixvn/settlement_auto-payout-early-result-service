using AutoPayout.Conditions.Services.HelperSerivce.VerifyFinalResult;
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
        var (setsResults, _) = await resultDetailService.GetResultsChecking(match, maxSetPlayed);

        var matchDetail = await repository.GetAsync<MatchDetail>(new GetMatchDetailCriteria { MatchId = match.MatchId });

        var isFinalConsistentWithGameSession = VerifyFinalResultHelper.FinalScoreIsValid(matchDetail, finalResult);
        var isValid = !setsResults.IsNullOrEmpty() &&
            isFinalConsistentWithGameSession
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
        await resultDetailService.UpdateResultStatus(match.MatchId, new List<ResultDetail> { finalResult });
        resultDetailService.WriteLogResult(
            match.MatchId,
            finalResult,
            message.Replace("@score", $"{finalResult.HomeValue}-{finalResult.AwayValue}"));
        match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != ResourceId.Final);
    }
}
