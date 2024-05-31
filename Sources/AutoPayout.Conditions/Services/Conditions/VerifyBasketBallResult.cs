using AutoPayout.Conditions.Services.HelperSerivce;
using AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;
using AutoPayout.Constants;

namespace AutoPayout.Conditions.Services;
public class VerifyBasketBallResult(ResultDetailService resultDetailService, IDynamicRepository repository, BasketballHelperService basketballHelper) : ICondition
{
    private const int MaxQuarter = 4;
    public async Task HandleCondition(Match match)
    {
        var matchDetail = await repository.GetAsync<MatchDetail>(new GetMatchDetailCriteria { MatchId = match.MatchId });

        await HandleQuarter(match, matchDetail.GameSession);

        await HandleFinal(match, matchDetail);

        match.MatchDetail = matchDetail;
    }

    private async Task HandleFinal(Match match, MatchDetail matchDetail)
    {
        var settledResults = await resultDetailService.GetResultSettled(match.MatchId, match.SportType);

        var resultCalculated = settledResults.Where(r => r.SettlementStatusId == SettlementStatusId.Calculated);

        var (results, _) = await resultDetailService.GetResultsChecking(match, MaxQuarter, resultCalculated);

        var isHalfTimeValid = await HandleHalfTime(match, matchDetail, results);

        await HandleFullTime(match, matchDetail, results, isHalfTimeValid);
    }

    private async Task<bool> HandleHalfTime(Match match, MatchDetail matchDetail, IReadOnlyCollection<ResultDetail> results)
    {
        var halfTime = match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.Halftime);
        var resultWithoutHt = results.Where(r => r.ResourceId != ResourceIds.Halftime).ToList();
        if (halfTime is not null && basketballHelper.IsHalfTimeInValid(halfTime, resultWithoutHt, matchDetail.GameSession, out var message))
        {
            await HandleInvalidResult(halfTime, match, message);
            return false;
        }

        return true;
    }

    private async Task HandleFullTime(Match match, MatchDetail matchDetail, List<ResultDetail> results, bool isHalfTimeValid)
    {
        var fullTimeResult = match.ResultDetails.FirstOrDefault(r => r.ResourceId == ResourceIds.FullTime);
        var halfTime = results.FirstOrDefault(r => isHalfTimeValid && r.ResourceId == ResourceIds.Halftime);
        var errorMessage = string.Empty;
        if (fullTimeResult is null)
        {
            return;
        }

        var isFinalInValid = matchDetail.GameSession switch
        {
            GameType.Bo1 => basketballHelper.IsFullTimeDraw(fullTimeResult, out errorMessage),
            GameType.Bo2 => basketballHelper.IsFinalOfBo2InValid(fullTimeResult, halfTime, out errorMessage),
            GameType.Bo4 => basketballHelper.IsFinalOfBo4InValid(fullTimeResult, results, out errorMessage),
            _ => true
        };

        if (isFinalInValid)
        {
            await HandleInvalidResult(fullTimeResult, match, errorMessage);
        }
    }

    private async Task HandleQuarter(Match match, int gameSession)
    {
        var quarter = match.ResultDetails.Where(r =>
            r.ResourceId != ResourceIds.FullTime && r.ResourceId != ResourceIds.Halftime).ToList();
        if (gameSession is GameType.Bo1 or GameType.Bo2 && quarter.Any())
        {
            var message = CreateMessageService.InconsistentWithGameSession(quarter, gameSession);
            await resultDetailService.UpdateResultStatus(match.MatchId, quarter, message);
            resultDetailService.WriteLogResults(
                match.MatchId,
                quarter,
                message);
            match.ResultDetails = match.ResultDetails.Except(quarter);
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
