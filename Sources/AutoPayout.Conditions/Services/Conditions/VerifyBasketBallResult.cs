using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;
using AutoPayout.Constants;

namespace AutoPayout.Conditions.Services;
public class VerifyBasketBallResult(ResultDetailService resultDetailService, IDynamicRepository repository, BasketballHelperService basketballHelper) : ICondition
{
    private const int MaxQuarter = 4;
    public async Task HandleCondition(Match match)
    {
        match.MatchDetail = await repository.GetAsync<MatchDetail>(new GetMatchDetailCriteria { MatchId = match.MatchId });


        await HandleQuarter(match);

        await HandleFinal(match);

    }

    private async Task HandleFinal(Match match)
    {
        var settledResults = await resultDetailService.GetResultSettled(match.MatchId, match.SportType);

        var resultCalculated = settledResults.Where(r => r.SettlementStatusId == SettlementStatusId.Calculated);

        var (results, _) = await resultDetailService.GetResultsChecking(match, MaxQuarter, resultCalculated);

        if (await HandleHalfTime(match, match.MatchDetail.GameSession, results))
        {
            results = results.Where(r => r.ResourceId != ResourceIds.Halftime).ToList();
        };

        await HandleFullTime(match, match.MatchDetail.GameSession, results);
    }

    private async Task<bool> HandleHalfTime(Match match, int gameSession, IReadOnlyCollection<ResultDetail> results)
    {
        var halfTime = match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.Halftime);
        var resultWithoutHt = results.Where(r => r.ResourceId != ResourceIds.Halftime).ToList();
        if (halfTime is not null && basketballHelper.IsHalfTimeInValid(halfTime, resultWithoutHt, gameSession, out var message))
        {
            await HandleInvalidResult(halfTime, match, message);
            return true;
        }

        return false;
    }

    private async Task HandleFullTime(Match match, int gameSession, List<ResultDetail> results)
    {
        var fullTimeResult = match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.FullTime);
        var halfTime = results.FirstOrDefault(r => r.ResourceId == ResourceIds.Halftime);
        if (fullTimeResult is null)
        {
            return;
        }

        var isFinalInValid = gameSession switch
        {
            GameType.Bo1 => basketballHelper.IsFullTimeDraw(fullTimeResult),
            GameType.Bo2 => basketballHelper.IsFinalOfBo2InValid(fullTimeResult, halfTime),
            GameType.Bo4 => await basketballHelper.IsFinalOfBo4InValid(match, fullTimeResult, results),
            _ => CreateMessageService.InconsistentWithGameSession(fullTimeResult, gameSession)
        };

        if (isFinalInValid != string.Empty)
        {
            await HandleInvalidResult(fullTimeResult, match, isFinalInValid);
        }
    }

    private async Task HandleQuarter(Match match)
    {
        var quarter = match.ResultDetails.Where(r =>
            r.ResourceId != ResourceIds.FullTime && r.ResourceId != ResourceIds.Halftime).ToList();
        if (match.MatchDetail.GameSession is GameType.Bo1 or GameType.Bo2 && quarter.Any())
        {
            var message = CreateMessageService.InconsistentWithGameSession(quarter, match.MatchDetail.GameSession);
            await resultDetailService.UpdateResultStatus(match.MatchId, quarter, message);
            resultDetailService.WriteLogResults(
                match.MatchId,
                quarter,
                message);
            match.ResultDetails = match.ResultDetails.Except(quarter).ToList();
        }
    }

    private async Task HandleInvalidResult(ResultDetail result, Match match, string message)
    {
        if (match.MatchDetail.GameSession == GameType.Bo4 && result.ResourceId == ResourceIds.FullTime)
        {
            await resultDetailService.UpdateResultStatus(match.MatchId, new List<ResultDetail> { result, new() { ResourceId = ResourceIds.OverTime } }, message);
        }
        else
        {
            await resultDetailService.UpdateResultStatus(match.MatchId, new List<ResultDetail> { result }, message);
        }
        resultDetailService.WriteLogResult(
            match.MatchId,
            result,
            message);
        match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != result.ResourceId).ToList();
    }
}
