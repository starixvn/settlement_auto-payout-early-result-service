using AutoPayout.Conditions.Services.HelperSerivce;
using AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;

namespace AutoPayout.Conditions.Services;
public class VerifyBasketBallFinalResult(ResultDetailService resultDetailService, IDynamicRepository repository) : ICondition
{
    private const int MaxQuarter = 4;
    public async Task HandleCondition(Match match)
    {
        var matchDetail = await repository.GetAsync<MatchDetail>(new GetMatchDetailCriteria { MatchId = match.MatchId });
        var settledResults = await resultDetailService.GetResultSettled(match.MatchId, match.SportType);

        var (results, _) = await resultDetailService.GetResultsChecking(match, MaxQuarter, settledResults);


        await HandleHalfTime(match, matchDetail, results);

    }

    private async Task HandleHalfTime(Match match, MatchDetail matchDetail, List<ResultDetail> results)
    {
        var halfTime = match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceId.Halftime);
        if (halfTime is not null && VerifyFinalResultHelper.IsHalfTimeInValidForBasketball(halfTime, results, matchDetail.GameSession, out var message))
        {
            await HandleInvalidResult(halfTime, match, message);
        }
    }

    private async Task HandleFullTime(Match match, MatchDetail matchDetail, List<ResultDetail> results)
    {
        var final = match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceId.Final);
        if (final is not null && VerifyFinalResultHelper.IsHalfTimeInValidForBasketball(final, results, matchDetail.GameSession, out var message))
        {
            await HandleInvalidResult(final, match, message);
        }
    }

    private async Task HandleInvalidResult(ResultDetail result, Match match, string message)
    {
        await resultDetailService.UpdateResultStatus(match.MatchId, new List<ResultDetail> { result }, message);
        resultDetailService.WriteLogResult(
            match.MatchId,
            result,
            message);
        match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != result.ResourceId);
    }
}
