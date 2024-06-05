using AutoPayout.Constants;

namespace AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;

public interface BasketballHelperService
{
    public bool IsHalfTimeInValid(ResultDetail halfTimeResult, List<ResultDetail> results, int gameSession,
        out string message);

    public bool IsFullTimeDraw(ResultDetail finalResult, out string message);

    public bool IsFinalOfBo2InValid(ResultDetail finalResult, ResultDetail halfTimeResult, out string message);

    public bool IsFinalOfBo4InValid(ResultDetail finalResult, List<ResultDetail> results, out string message);
}
public class BasketballHelperServiceImp : BasketballHelperService
{
    public bool IsHalfTimeInValid(ResultDetail halfTimeResult, List<ResultDetail> results, int gameSession, out string message)
    {
        switch (gameSession)
        {
            case GameType.Bo1:
                message = CreateMessageService.HalfTimeInconsistentWithGameSession(halfTimeResult, gameSession);
                return true;
            case GameType.Bo4 when HalfTimeRelatedResultInValid(halfTimeResult, results, out message):
            case GameType.Bo4 when IsHalfTimeScoreInValid(halfTimeResult, results, out message):
                return true;
            default:
                message = string.Empty;
                return false;
        }
    }

    public bool IsFullTimeDraw(ResultDetail finalResult, out string message)
    {
        if (int.Parse(finalResult.HomeValue) == int.Parse(finalResult.AwayValue))
        {
            message = CreateMessageService.FullTimeDrawMessage(finalResult);
            return true;
        }

        message = string.Empty;
        return false;
    }


    public bool IsFinalOfBo2InValid(ResultDetail finalResult, ResultDetail halfTimeResult, out string message)
    {
        if (IsFullTimeDraw(finalResult, out message))
        {
            return true;
        }

        if (halfTimeResult is null)
        {
            message = CreateMessageService.HalfTimeNotCalculated(finalResult);
            return true;
        }

        if (int.Parse(halfTimeResult.HomeValue) > int.Parse(finalResult.HomeValue) ||
            int.Parse(halfTimeResult.AwayValue) > int.Parse(finalResult.AwayValue))
        {
            message = CreateMessageService.HalfTimeInconsistentWithFT(finalResult, halfTimeResult);
            return true;
        }

        finalResult.RelatedInfo = $"{halfTimeResult.HomeValue}-{halfTimeResult.AwayValue}";

        message = string.Empty;
        return false;
    }

    public bool IsFinalOfBo4InValid(ResultDetail finalResult, List<ResultDetail> results, out string message)
    {
        if (IsFullTimeDraw(finalResult, out message))
        {
            return true;
        }

        if (FullTimeRelatedResultInValid(finalResult, results, out message))
        {
            return true;
        }

        if (FullTimeRelatedResultScoreInvalid(finalResult, results, out message))
        {
            return true;
        }

        message = string.Empty;
        return false;
    }


    private static bool FullTimeRelatedResultInValid(ResultDetail finalResult, IReadOnlyCollection<ResultDetail> results, out string message)
    {
        var resultsValid = ResourceIds.ResultFtBasketBall.Where(r => results.Any(rd => rd.ResourceId == r)).ToArray();
        var isOtExist = resultsValid.Any(r => r == ResourceIds.OverTime);
        var relatedResultNeeded = isOtExist ? ResourceIds.ResultFtBasketBall.Length : ResourceIds.ResultFtWithoutOTBasketBall.Length;

        message = resultsValid.Length != relatedResultNeeded ?
            CreateMessageService.RelatedResultInvalid(finalResult, resultsValid, isOtExist ? ResourceIds.ResultFtBasketBall : ResourceIds.ResultFtWithoutOTBasketBall)
            : string.Empty;
        return resultsValid.Length != relatedResultNeeded;
    }

    private static bool FullTimeRelatedResultScoreInvalid(ResultDetail finalResult, List<ResultDetail> results, out string message)
    {
        var quarterResult = results.Where(r => r.ResourceId != ResourceIds.Halftime).ToList();
        var totalHomeValue = SumHomeValueFullTime(quarterResult);
        var totalAwayValue = SumAwayValueFullTime(quarterResult);
        var otResult = results.FirstOrDefault(r => r.ResourceId == ResourceIds.OverTime);
        var isOTInvalid = IsOtInValid(otResult, totalHomeValue, totalAwayValue);
        if (isOTInvalid || IsTotalScoreInValid(finalResult, totalHomeValue, totalAwayValue, otResult))
        {
            message = CreateMessageService.ResultRelatedInconsistent(finalResult, results);
            return true;
        }

        message = string.Empty;
        return false;
    }

    private static bool IsOtInValid(ResultDetail otResult, int totalHomeValue, int totalAwayValue)
    {
        var isOtExist = otResult != null && string.CompareOrdinal($"{otResult.HomeValue}:{otResult.AwayValue}", "0:0") != 0;

        if (isOtExist && (totalHomeValue != totalAwayValue || int.Parse(otResult.HomeValue) == int.Parse(otResult.AwayValue)))
        {
            return true;
        }

        return !isOtExist && totalHomeValue == totalAwayValue;
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
