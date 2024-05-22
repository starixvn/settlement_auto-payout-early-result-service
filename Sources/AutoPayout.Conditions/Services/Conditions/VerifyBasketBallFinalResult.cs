using AutoPayout.Conditions.Services.HelperSerivce;
using AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;
using AutoPayout.Constants;

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

        await HandleFullTime(match, matchDetail, results);

        match.MatchDetail = matchDetail;

    }

    private async Task HandleHalfTime(Match match, MatchDetail matchDetail, List<ResultDetail> results)
    {
        var halfTime = match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.Halftime);
        var resultWithoutHt = results.Where(r => r.ResourceId != ResourceIds.Halftime).ToList();
        if (halfTime is not null && VerifyFinalResultForBasketballHelper.IsHalfTimeInValid(halfTime, resultWithoutHt, matchDetail.GameSession, out var message))
        {
            await HandleInvalidResult(halfTime, match, message);
        }
    }

    private async Task HandleFullTime(Match match, MatchDetail matchDetail, List<ResultDetail> results)
    {
        var fullTimeResult = match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.FullTime);
        var halfTime = results.FirstOrDefault(r => r.ResourceId == ResourceIds.Halftime);
        var errorMessage = string.Empty;
        if (fullTimeResult is null)
        {
            return;
        }

        var isFinalInValid = matchDetail.GameSession switch
        {
            GameType.Bo1 => VerifyFinalResultForBasketballHelper.IsFullTimeDraw(fullTimeResult, out errorMessage),
            GameType.Bo2 => VerifyFinalResultForBasketballHelper.IsFinalOfBo2InValid(fullTimeResult, halfTime, out errorMessage),
            GameType.Bo4 => VerifyFinalResultForBasketballHelper.IsFinalOfBo4InValid(fullTimeResult, results, out errorMessage),
            _ => true
        };

        if (isFinalInValid)
        {
            await HandleInvalidResult(fullTimeResult, match, errorMessage);
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
