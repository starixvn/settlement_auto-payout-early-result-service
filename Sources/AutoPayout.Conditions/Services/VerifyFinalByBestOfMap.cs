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

        var matchDetail = await repository.GetAsync<MatchDetail>(new GetMatchDetailCriteria { MatchId = match.MatchId });
        var settledResults = await resultDetailService.GetResultSettled(match.MatchId, match.SportType);

        var maxGamePlayed = int.Parse(finalResult.HomeValue) + int.Parse(finalResult.AwayValue);
        var (results, resultIndexes) = await resultDetailService.GetResultsChecking(match, maxGamePlayed, settledResults);

        var isValid = ValidateResult(results, matchDetail, finalResult, settledResults);

        if (isValid)
        {
            match.ResourceRefund = GetResourceToRefund(settledResults, resultIndexes, matchDetail);
        }
        else
        {
            await HandleFinalResult(finalResult, match);
        }
    }

    private string GetResourceToRefund(IReadOnlyList<ResultDetail> resultSettled, IReadOnlyList<ResourceIndex> resourceIndexes, MatchDetail matchDetail)
    {
        if (matchDetail.BestOfMap is Bo1 or Bo2)

        {
            return string.Empty;
        }

        var resourceToRefund = new List<string>();
        var resourceSettled = resultSettled.Select(x => x.ResourceId).ToList();
        var totalResourcePlayed = TotalGamePlayed(matchDetail);

        for (var i = totalResourcePlayed + 1; i <= matchDetail.BestOfMap; i++)
        {
            var resource = resourceIndexes.FirstOrDefault(r => r.IntervalNo == i);
            if (resource is not null && !resourceSettled.Contains(resource.ResourceId))
            {
                resourceToRefund.Add(resource.ResourceId);
            }
        }

        return string.Join(",", resourceToRefund);
    }

    private bool ValidateResult(List<ResultDetail> results, MatchDetail matchDetail, ResultDetail finalResult, List<ResultDetail> settledResults)
    {
        return !results.IsNullOrEmpty() && matchDetail.BestOfMap switch
        {
            Bo1 => VerifyFinalResultHelper.VerifyFinalBO1(finalResult, results),
            Bo2 => VerifyFinalResultHelper.VerifyFinalBestOfTWO(finalResult, results) &&
                   VerifyFinalResultHelper.VerifyResult(results, finalResult),
            _ => VerifyFinalResultHelper.VerifyResultSettled(results, settledResults, TotalGamePlayed(matchDetail))
                    && VerifyFinalResultHelper.FinalScoreIsValid(matchDetail, finalResult, false)
                    && VerifyFinalResultHelper.IsMatchResultsConsecutive(results)
                    && VerifyFinalResultHelper.VerifyResult(results, finalResult),
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

    private int TotalGamePlayed(MatchDetail matchDetail)
    {
        var total = matchDetail.BestOfMap / 2 + 1;
        return total;
    }
}