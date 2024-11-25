using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Helpers;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;

public interface BasketballHelperService
{
    Task<string> VerifyHalfTimeInValid(Match match, ResultDetail halfTimeResult, List<ResultDetail> resultDetails, int gameSession);

    string FullTimeDraw(ResultDetail finalResult);

    Task<string> FinalOfBo2InValid(Match match, ResultDetail finalResult, ResultDetail halfTimeResult);

    Task<string> FinalOfBo4InValid(Match match, ResultDetail finalResult, List<ResultDetail> results);
}

public class BasketballHelperServiceImp(
    ResultDetailService resultDetailService,
    ExtraResultService overTimeService,
    IOptions<AppSettingsKey> appSettingsKey) : BasketballHelperService
{
    public async Task<string> VerifyHalfTimeInValid(Match match, ResultDetail halfTimeResult, List<ResultDetail> resultDetails, int gameSession)
    {
        if (gameSession == GameType.Bo1)
        {
            return CreateMessageService.InconsistentWithGameSession(halfTimeResult, gameSession);
        }

        return gameSession == GameType.Bo4
            ? await VerifyHalfTimeOfBo4InValid(match, halfTimeResult, resultDetails)
            : string.Empty;
    }

    private async Task<string> VerifyHalfTimeOfBo4InValid(Match match, ResultDetail halfTimeResult, IReadOnlyCollection<ResultDetail> resultDetails)
    {
        var resultsValid = ResourceIds.ResultHtBasketBall.Where(r => resultDetails.FirstOrDefault(rd => rd.ResourceId == r) != null).ToArray();

        if (resultsValid.Length != ResourceIds.ResultHtBasketBall.Length)
        {
            return await HalfTimeRelatedResultInValidAndMaxTimeScore(match, halfTimeResult, resultsValid);
        }

        return HalfTimeScoreInValid(halfTimeResult, resultDetails);
    }

    private async Task<string> HalfTimeRelatedResultInValidAndMaxTimeScore(Match match, ResultDetail halfTimeResult, IEnumerable<string> resultsValid)
    {
        var maxTimeScore = await resultDetailService.MaxTimeScore(halfTimeResult);
        if (maxTimeScore > appSettingsKey.Value.RetryProcessScore)
        {
            return CreateMessageService.RelatedResultInvalid(halfTimeResult, resultsValid, ResourceIds.ResultHtBasketBall);
        }

        match.ResultDetails.Remove(halfTimeResult);
        return string.Empty;
    }

    private static string HalfTimeScoreInValid(ResultDetail halfTimeResult, IEnumerable<ResultDetail> results)
    {
        var resultRelated = results.Where(r => ResourceIds.ResultHtBasketBall.Contains(r.ResourceId)).ToList();

        var isInconsistentScoreHalfTime = IsInconsistentScoreHalfTime(halfTimeResult, resultRelated);

        return isInconsistentScoreHalfTime ? CreateMessageService.ResultRelatedInconsistent(halfTimeResult, resultRelated, ResourceIds.OverTime, true) : string.Empty;
    }

    private static bool IsInconsistentScoreHalfTime(ResultDetail halfTimeResult, IEnumerable<ResultDetail> resultRelated)
    {
        var home = resultRelated.Select(r => int.Parse(r.HomeValue)).Sum();
        var away = resultRelated.Select(r => int.Parse(r.AwayValue)).Sum();

        return home != int.Parse(halfTimeResult.HomeValue) || away != int.Parse(halfTimeResult.AwayValue);
    }

    public string FullTimeDraw(ResultDetail finalResult)
    {
        return int.Parse(finalResult.HomeValue) == int.Parse(finalResult.AwayValue) ? CreateMessageService.FullTimeDrawMessage(finalResult) : string.Empty;
    }

    public async Task<string> FinalOfBo2InValid(Match match, ResultDetail finalResult, ResultDetail halfTimeResult)
    {
        var isDraw = FullTimeDraw(finalResult);
        if (isDraw != string.Empty)
        {
            return isDraw;
        }

        if (halfTimeResult is null)
        {
            return await FinalOfBo2RelatedResultInValidAndMaxTimeScore(match, finalResult);
        }

        var isInValidScore = int.Parse(halfTimeResult.HomeValue) > int.Parse(finalResult.HomeValue) ||
                            int.Parse(halfTimeResult.AwayValue) > int.Parse(finalResult.AwayValue);

        if (isInValidScore)
        {
            return CreateMessageService.HalfTimeInconsistentWithFT(finalResult, halfTimeResult);
        }

        finalResult.RelatedInfo = $"{halfTimeResult.HomeValue}-{halfTimeResult.AwayValue}";

        return string.Empty;
    }

    private async Task<string> FinalOfBo2RelatedResultInValidAndMaxTimeScore(Match match, ResultDetail finalResult)
    {
        var maxTimeScore = await resultDetailService.MaxTimeScore(finalResult);
        if (maxTimeScore > appSettingsKey.Value.RetryProcessScore)
        {
            return CreateMessageService.HalfTimeNotCalculated(finalResult);
        }

        match.ResultDetails.Remove(finalResult);
        return string.Empty;
    }

    public async Task<string> FinalOfBo4InValid(Match match, ResultDetail finalResult, List<ResultDetail> results)
    {
        var isDraw = FullTimeDraw(finalResult);
        if (isDraw != string.Empty)
        {
            return isDraw;
        }

        var resultsValid = ResourceIds.ResultFtWithoutOtBasketBall.Where(r => results.Exists(rd => rd.ResourceId == r)).ToArray();

        if (resultsValid.Length != ResourceIds.ResultFtWithoutOtBasketBall.Length)
        {
            return await FullTimeRelatedResultInValidAndMaxTimeScore(match, finalResult, resultsValid);
        }

        var isInvalid = await FullTimeRelatedResultScoreInvalid(match, finalResult, results);
        if (isInvalid)
        {
            return CreateMessageService.ResultRelatedInconsistent(finalResult, results, ResourceIds.OverTime);
        }

        match.ResultDetails.AddIf(results.TryGetValue(r => r.ResourceId == ResourceIds.OverTime && r.IsValid, out var overTime), overTime);
        return string.Empty;
    }

    private async Task<string> FullTimeRelatedResultInValidAndMaxTimeScore(Match match, ResultDetail finalResult, IEnumerable<string> resultsValid)
    {
        var maxTimeScore = await resultDetailService.MaxTimeScore(finalResult);
        if (maxTimeScore > appSettingsKey.Value.RetryProcessScore)
        {
            return CreateMessageService.RelatedResultInvalid(finalResult, resultsValid, ResourceIds.ResultFtWithoutOtBasketBall);
        }

        match.ResultDetails.Remove(finalResult);
        return string.Empty;
    }

    private async Task<bool> FullTimeRelatedResultScoreInvalid(Match match, ResultDetail finalResult, List<ResultDetail> results)
    {
        var quarterResult = results.Where(r => r.ResourceId != ResourceIds.Halftime).ToList();
        var totalHomeValue = SumHomeValueFullTime(quarterResult);
        var totalAwayValue = SumAwayValueFullTime(quarterResult);
        var otResult = await overTimeService.GetOtBasketBall(finalResult, totalHomeValue, totalAwayValue);
        results.Add(otResult);

        if (otResult.NeedRetry)
        {
            match.ResultDetails.Remove(finalResult);
            return false;
        }

        var isTotalScoreInValid = resultDetailService.InValidTotalWithExtraScore(finalResult, otResult, totalHomeValue, totalAwayValue);
        return !otResult.IsValid || isTotalScoreInValid;
    }


    private static int SumHomeValueFullTime(IEnumerable<ResultDetail> results)
    {
        return results
            .Where(r => r.ResourceId != ResourceIds.OverTime && r.ResourceId != ResourceIds.Halftime)
            .Select(r => int.Parse(r.HomeValue)).Sum();
    }

    private static int SumAwayValueFullTime(IEnumerable<ResultDetail> results)
    {
        return results
            .Where(r => r.ResourceId != ResourceIds.OverTime && r.ResourceId != ResourceIds.Halftime)
            .Select(r => int.Parse(r.AwayValue)).Sum();
    }
}