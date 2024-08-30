using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;
using AutoPayout.Constants;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyBasketBallResult(
    ResultDetailService resultDetailService,
    IDynamicRepository repository,
    BasketballHelperService basketballHelper) : ICondition
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

        await HandleHalfTime(match, match.MatchDetail.GameSession, results);

        await HandleFullTime(match, match.MatchDetail.GameSession, results);
    }

    private async Task HandleHalfTime(Match match, int gameSession, List<ResultDetail> results)
    {
        var halfTime = match.ResultDetails.Find(r => r.ResourceId == ResourceIds.Halftime);
        var resultWithoutHt = results.Where(r => r.ResourceId != ResourceIds.Halftime).ToList();
        if (halfTime is null)
        {
            return;
        }

        var halfTimeMessage = await basketballHelper.VerifyHalfTimeInValid(match, halfTime, resultWithoutHt, gameSession);

        if (!string.IsNullOrEmpty(halfTimeMessage))
        {
            await HandleInvalidResult(halfTime, match, halfTimeMessage);
            results.Remove(halfTime);
        }
    }

    private async Task HandleFullTime(Match match, int gameSession, List<ResultDetail> results)
    {
        var fullTimeResult = match.ResultDetails.Find(r => r.ResourceId == ResourceIds.FullTime);
        var halfTime = results.Find(r => r.ResourceId == ResourceIds.Halftime);
        if (fullTimeResult is null)
        {
            return;
        }

        var errorMessage = gameSession switch
        {
            GameType.Bo1 => basketballHelper.FullTimeDraw(fullTimeResult),
            GameType.Bo2 => await basketballHelper.FinalOfBo2InValid(match, fullTimeResult, halfTime),
            GameType.Bo4 => await basketballHelper.FinalOfBo4InValid(match, fullTimeResult, results),
            _ => CreateMessageService.InconsistentWithGameSession(fullTimeResult, gameSession)
        };

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            await HandleInvalidResult(fullTimeResult, match, errorMessage);
        }
    }

    private async Task HandleQuarter(Match match)
    {
        var quarter = match.ResultDetails.Where(r =>
            r.ResourceId != ResourceIds.FullTime && r.ResourceId != ResourceIds.Halftime).ToList();
        if (match.MatchDetail.GameSession is GameType.Bo1 or GameType.Bo2 && quarter.Any())
        {
            var message = CreateMessageService.ResultsInconsistentWithGameSession(quarter, match.MatchDetail.GameSession);
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

        match.ResultDetails.Remove(result);
    }
}