using AutoPayout.Conditions.Helpers;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;

namespace AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;

public interface BasketballHelperService
{
    public bool IsHalfTimeInValid(ResultDetail halfTimeResult, List<ResultDetail> results, int gameSession,
        out string message);

    public string IsFullTimeDraw(ResultDetail finalResult);

    public string IsFinalOfBo2InValid(ResultDetail finalResult, ResultDetail halfTimeResult);

    public Task<string> IsFinalOfBo4InValid(Match match, ResultDetail finalResult, List<ResultDetail> results);
}
public class BasketballHelperServiceImp(OverTimeService overTimeService) : BasketballHelperService
{
    public bool IsHalfTimeInValid(ResultDetail halfTimeResult, List<ResultDetail> results, int gameSession, out string message)
    {
        switch (gameSession)
        {
            case GameType.Bo1:
                message = CreateMessageService.InconsistentWithGameSession(halfTimeResult, gameSession);
                return true;
            case GameType.Bo4 when HalfTimeRelatedResultInValid(halfTimeResult, results, out message):
            case GameType.Bo4 when IsHalfTimeScoreInValid(halfTimeResult, results, out message):
                return true;
            default:
                message = string.Empty;
                return false;
        }
    }

    public string IsFullTimeDraw(ResultDetail finalResult)
    {
        return int.Parse(finalResult.HomeValue) == int.Parse(finalResult.AwayValue) ? CreateMessageService.FullTimeDrawMessage(finalResult) : string.Empty;
    }


    public string IsFinalOfBo2InValid(ResultDetail finalResult, ResultDetail halfTimeResult)
    {
        var isDraw = IsFullTimeDraw(finalResult);
        if (isDraw != string.Empty)
        {
            return isDraw;
        }

        if (halfTimeResult is null)
        {
            return CreateMessageService.HalfTimeNotCalculated(finalResult);
        }

        if (int.Parse(halfTimeResult.HomeValue) > int.Parse(finalResult.HomeValue) ||
            int.Parse(halfTimeResult.AwayValue) > int.Parse(finalResult.AwayValue))
        {
            return CreateMessageService.HalfTimeInconsistentWithFT(finalResult, halfTimeResult);

        }

        finalResult.RelatedInfo = $"{halfTimeResult.HomeValue}-{halfTimeResult.AwayValue}";

        return string.Empty;
    }

    public async Task<string> IsFinalOfBo4InValid(Match match, ResultDetail finalResult, List<ResultDetail> results)
    {
        var isDraw = IsFullTimeDraw(finalResult);
        if (isDraw != string.Empty)
        {
            return isDraw;
        }

        if (FullTimeRelatedResultInValid(finalResult, results, out var message))
        {
            return message;
        }

        if (await FullTimeRelatedResultScoreInvalid(match, finalResult, results))
        {
            return CreateMessageService.ResultRelatedInconsistent(finalResult, results);
        }

        match.ResultDetails.AddIf(results.TryGetValue(r => r.ResourceId == ResourceIds.OverTime && r.IsValid, out var overTime), overTime);
        return string.Empty;
    }

    private static bool FullTimeRelatedResultInValid(ResultDetail finalResult, IReadOnlyCollection<ResultDetail> results, out string message)
    {
        var resultsValid = ResourceIds.ResultFtWithoutOTBasketBall.Where(r => results.Any(rd => rd.ResourceId == r)).ToArray();

        message = resultsValid.Length != ResourceIds.ResultFtWithoutOTBasketBall.Length ? CreateMessageService.RelatedResultInvalid(finalResult, resultsValid, ResourceIds.ResultFtWithoutOTBasketBall) : string.Empty;
        return resultsValid.Length != ResourceIds.ResultFtWithoutOTBasketBall.Length;
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

        return !otResult.IsValid || IsTotalScoreInValid(finalResult, totalHomeValue, totalAwayValue, otResult);
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

    private static bool HalfTimeRelatedResultInValid(ResultDetail halfTimeResult, IEnumerable<ResultDetail> results, out string message)
    {
        var resultsValid = ResourceIds.ResultHtBasketBall.Where(r => results.FirstOrDefault(rd => rd.ResourceId == r) != null).ToArray();

        if (resultsValid.Length != ResourceIds.ResultHtBasketBall.Length)
        {
            message = CreateMessageService.RelatedResultInvalid(halfTimeResult, resultsValid, ResourceIds.ResultHtBasketBall);
            return true;
        }

        message = string.Empty;
        return false;
    }

    private static bool IsHalfTimeScoreInValid(ResultDetail halfTimeResult, IEnumerable<ResultDetail> results, out string message)
    {
        var resultRelated = results.Where(r => ResourceIds.ResultHtBasketBall.Contains(r.ResourceId)).ToList();

        var home = resultRelated.Select(r => int.Parse(r.HomeValue)).Sum();
        var away = resultRelated.Select(r => int.Parse(r.AwayValue)).Sum();

        if (home != int.Parse(halfTimeResult.HomeValue) || away != int.Parse(halfTimeResult.AwayValue))
        {
            message = CreateMessageService.ResultRelatedInconsistent(halfTimeResult, resultRelated);
            return true;
        }

        message = string.Empty;
        return false;
    }
}
