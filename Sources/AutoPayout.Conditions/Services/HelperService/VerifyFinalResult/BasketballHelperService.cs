using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Helpers;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;

public interface BasketballHelperService
{
    Task<string> VerifyHalfTimeInValid(ResultDetail halfTimeResult, List<ResultDetail> resultDetails, int gameSession);

    string FullTimeDraw(ResultDetail finalResult);

    Task<string> FinalOfBo2InValid(ResultDetail finalResult, ResultDetail halfTimeResult);

    Task<string> FinalOfBo4InValid(Match match, ResultDetail finalResult, List<ResultDetail> results);
}

public class BasketballHelperServiceImp(
    ResultDetailService resultDetailService,
    OverTimeService overTimeService,
    IOptions<AppSettingsKey> appSettingsKey) : BasketballHelperService
{
    public async Task<string> VerifyHalfTimeInValid(ResultDetail halfTimeResult, List<ResultDetail> resultDetails, int gameSession)
    {
        if (gameSession == GameType.Bo1)
        {
            return CreateMessageService.InconsistentWithGameSession(halfTimeResult, gameSession);
        }

        return gameSession == GameType.Bo4
            ? await VerifyHalfTimeOfBo4InValid(halfTimeResult, resultDetails)
            : string.Empty;
    }

    private async Task<string> VerifyHalfTimeOfBo4InValid(ResultDetail halfTimeResult, IReadOnlyCollection<ResultDetail> resultDetails)
    {
        var (isInValidResult, errorMessageResult) = await HalfTimeRelatedResultInValid(halfTimeResult, resultDetails);
        if (isInValidResult)
        {
            return errorMessageResult;
        }

        var (isInValidScore, errorMessageScore) = HalfTimeScoreInValid(halfTimeResult, resultDetails);
        return isInValidScore ? errorMessageScore : string.Empty;
    }

    public string FullTimeDraw(ResultDetail finalResult)
    {
        return int.Parse(finalResult.HomeValue) == int.Parse(finalResult.AwayValue) ? CreateMessageService.FullTimeDrawMessage(finalResult) : string.Empty;
    }

    public async Task<string> FinalOfBo2InValid(ResultDetail finalResult, ResultDetail halfTimeResult)
    {
        var isDraw = FullTimeDraw(finalResult);
        if (isDraw != string.Empty)
        {
            return isDraw;
        }

        if (halfTimeResult is null)
        {
            var maxTimeScore = await MaxTimeScore(finalResult);
            return maxTimeScore > appSettingsKey.Value.RetryProcessScore ? CreateMessageService.HalfTimeNotCalculated(finalResult) : string.Empty;
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

    public async Task<string> FinalOfBo4InValid(Match match, ResultDetail finalResult, List<ResultDetail> results)
    {
        var isDraw = FullTimeDraw(finalResult);
        if (isDraw != string.Empty)
        {
            return isDraw;
        }

        var (isInValidResult, errorMessageResult) = await FullTimeRelatedResultInValid(finalResult, results);
        if (isInValidResult)
        {
            return errorMessageResult;
        }

        var isInvalid = await FullTimeRelatedResultScoreInvalid(match, finalResult, results);
        if (isInvalid)
        {
            return CreateMessageService.ResultRelatedInconsistent(finalResult, results);
        }

        match.ResultDetails.AddIf(results.TryGetValue(r => r.ResourceId == ResourceIds.OverTime && r.IsValid, out var overTime), overTime);
        return string.Empty;
    }

    private async Task<(bool, string)> FullTimeRelatedResultInValid(ResultDetail finalResult, IReadOnlyCollection<ResultDetail> results)
    {
        var resultsValid = ResourceIds.ResultFtWithoutOTBasketBall.Where(r => results.Any(rd => rd.ResourceId == r)).ToArray();
        var maxTimeScore = await MaxTimeScore(finalResult);

        if (resultsValid.Length != ResourceIds.ResultFtWithoutOTBasketBall.Length &&
            maxTimeScore > appSettingsKey.Value.RetryProcessScore)
        {
            var message = CreateMessageService.RelatedResultInvalid(finalResult, resultsValid,
                ResourceIds.ResultFtWithoutOTBasketBall);
            return (true, message);
        }

        return (false, string.Empty);
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

        var isTotalScoreInValid = IsTotalScoreInValid(finalResult, totalHomeValue, totalAwayValue, otResult);
        return !otResult.IsValid || isTotalScoreInValid;
    }

    private static bool IsTotalScoreInValid(ResultDetail finalResult, int totalHomeValue, int totalAwayValue, ResultDetail otResult)
    {
        totalHomeValue += otResult == null ? 0 : int.Parse(otResult.HomeValue);
        totalAwayValue += otResult == null ? 0 : int.Parse(otResult.AwayValue);

        return totalHomeValue != int.Parse(finalResult.HomeValue) || totalAwayValue != int.Parse(finalResult.AwayValue);
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

    private async Task<(bool, string)> HalfTimeRelatedResultInValid(ResultDetail halfTimeResult, IEnumerable<ResultDetail> results)
    {
        var resultsValid = ResourceIds.ResultHtBasketBall.Where(r => results.FirstOrDefault(rd => rd.ResourceId == r) != null).ToArray();
        var maxTimeScore = await MaxTimeScore(halfTimeResult);

        if (resultsValid.Length != ResourceIds.ResultHtBasketBall.Length && maxTimeScore > appSettingsKey.Value.RetryProcessScore)
        {
            var message = CreateMessageService.RelatedResultInvalid(halfTimeResult, resultsValid, ResourceIds.ResultHtBasketBall);
            return (true, message);
        }

        return (false, string.Empty);
    }

    private static (bool, string) HalfTimeScoreInValid(ResultDetail halfTimeResult, IEnumerable<ResultDetail> results)
    {
        var resultRelated = results.Where(r => ResourceIds.ResultHtBasketBall.Contains(r.ResourceId)).ToList();

        var isInconsistentScoreHalfTime = IsInconsistentScoreHalfTime(halfTimeResult, resultRelated);

        if (isInconsistentScoreHalfTime)
        {
            var message = CreateMessageService.ResultRelatedInconsistent(halfTimeResult, resultRelated);
            return (true, message);
        }

        return (false, string.Empty);
    }

    private static bool IsInconsistentScoreHalfTime(ResultDetail halfTimeResult, IEnumerable<ResultDetail> resultRelated)
    {
        var home = resultRelated.Select(r => int.Parse(r.HomeValue)).Sum();
        var away = resultRelated.Select(r => int.Parse(r.AwayValue)).Sum();

        return home != int.Parse(halfTimeResult.HomeValue) || away != int.Parse(halfTimeResult.AwayValue);
    }

    private async Task<double> MaxTimeScore(ResultDetail resultDetail)
    {
        var scoreInfo = (await resultDetailService.GetSingleScore(resultDetail)).ToList();
        var maxDateScoresFinal = scoreInfo.Max(t => t.CreateDate);
        var processTime = DateTimeOffset.Now - maxDateScoresFinal;
        return processTime.TotalMilliseconds;
    }
}