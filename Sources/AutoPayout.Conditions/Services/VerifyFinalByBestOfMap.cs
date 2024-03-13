using AutoPayout.Conditions.Services.HelperSerivce.VerifyFinalResult;
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

        var maxGamePlayed = int.Parse(finalResult.HomeValue) + int.Parse(finalResult.AwayValue);
        var (results, resultIndexes) = await resultDetailService.GetResultsChecking(match, maxGamePlayed);

        var matchDetail = await repository.GetAsync<MatchDetail>(new GetMatchDetailCriteria { MatchId = match.MatchId });

        var isValid = ValidateResult(results, matchDetail, finalResult);


        if (isValid)
        {
            match.ResourceRefund = GetResourceToRefund(resultIndexes, finalResult, matchDetail.BestOfMap);
        }
        else
        {
            await HandleFinalResult(finalResult, match);
        }
    }

    private string GetResourceToRefund(IReadOnlyList<ResourceIndex> resourceIndexes, ResultDetail finalResult, int bestOfMatch)
    {
        if (bestOfMatch is Bo1 or Bo2)
        {
            return string.Empty;
        }
        var totalResourcePlayed = Convert.ToInt32(finalResult.HomeValue) + Convert.ToInt32(finalResult.AwayValue);
        var resourceToRefund = new List<string>();
        for (var i = totalResourcePlayed + 1; i <= bestOfMatch; i++)
        {
            var resource = resourceIndexes.FirstOrDefault(r => r.IntervalNo == i);
            resourceToRefund.Add(resource is not null ? resource.ResourceId : string.Empty);
        }

        return string.Join(",", resourceToRefund);

    }

    private bool ValidateResult(List<ResultDetail> results, MatchDetail matchDetail, ResultDetail finalResult)
    {
        return !results.IsNullOrEmpty() && matchDetail.BestOfMap switch
        {
            Bo1 => VerifyFinalResultHelper.VerifyFinalBO1(finalResult, results),
            Bo2 => VerifyFinalResultHelper.VerifyFinalBestOfTWO(finalResult, results) &&
                   VerifyFinalResultHelper.VerifyResult(results, finalResult),
            _ => VerifyFinalResultHelper.FinalScoreIsValid(matchDetail, finalResult, false) &&
                 VerifyFinalResultHelper.IsMatchResultsConsecutive(results) &&
                 VerifyFinalResultHelper.VerifyResult(results, finalResult),
        };
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
