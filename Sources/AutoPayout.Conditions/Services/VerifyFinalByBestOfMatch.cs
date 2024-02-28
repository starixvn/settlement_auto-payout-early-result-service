using AutoPayout.Conditions.Services.HelperSerivce.VerifyFinalResult;
using Microsoft.IdentityModel.Tokens;

namespace AutoPayout.Conditions.Services;
public class VerifyFinalByBestOfMatch(IDynamicRepository repository, ResultDetailService resultDetailService) : ICondition
{
    public async Task HandleCondition(Match match)
    {
        var finalResult = match.ResultDetails.FirstOrDefault(i => i.ResourceId == ResourceId.Final);
        if (match.ResourceType != ResourceType.MainResource || finalResult is null)
        {
            return;
        }

        var results = await resultDetailService.GetResultsChecking(match);

        var matchDetail = await repository.GetAsync<MatchDetail>(new GetMatchDetailCriteria { MatchId = match.MatchId });

        var isValid = !results.IsNullOrEmpty() && matchDetail.BestOfMatch switch
        {
            1 => VerifyFinalResultHelper.VerifyFinalBO1(finalResult, results),
            2 => VerifyFinalResultHelper.VerifyFinalBestOfTWO(finalResult, results) &&
            VerifyFinalResultHelper.VerifyResult(results, finalResult),
            _ => VerifyFinalResultHelper.FinalScoreIsValid(matchDetail, finalResult, false) &&
                 VerifyFinalResultHelper.IsMatchResultsConsecutive(results) &&
                 VerifyFinalResultHelper.VerifyResult(results, finalResult),
        };


        if (!isValid)
        {
            await HandleFinalResult(finalResult, match);
        }
    }


    private async Task HandleFinalResult(ResultDetail finalResult, Match match)
    {
        await resultDetailService.UpdateResultStatus(match.MatchId, new List<ResultDetail> { finalResult });
        resultDetailService.WriteLogResult(
            match.MatchId,
            finalResult,
            ErrorMessage.FinalESportsInconsistent.Replace("@score", $"{finalResult.HomeValue}-{finalResult.AwayValue}"));
        match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != ResourceId.Final);
    }
}
