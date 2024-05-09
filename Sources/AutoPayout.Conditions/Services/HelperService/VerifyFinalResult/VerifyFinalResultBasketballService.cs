using AutoPayout.Conditions.Services.HelperSerivce;

namespace AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;
public static class VerifyFinalResultForBasketballHelper
{
    public static bool IsHalfTimeInValid(ResultDetail halfTimeResult, List<ResultDetail> results, int gameSession, out string message)
    {
        if (gameSession == GameType.Bo1)
        {
            message = CreateMessageService.HalfTimeInconsistentWithGameSession(halfTimeResult, gameSession);
            return true;
        }

        if (gameSession == GameType.Bo4 && HalfTimeRelatedResultInValid(halfTimeResult, results, out message))
        {
            return true;
        }

        if (gameSession == GameType.Bo4 && IsHalfTimeScoreInValid(halfTimeResult, results, out message))
        {
            return true;
        }

        message = string.Empty;
        return false;
    }

    public static bool IsFullTimeDraw(ResultDetail finalResult, out string message)
    {
        if (int.Parse(finalResult.HomeValue) == int.Parse(finalResult.AwayValue))
        {
            message = CreateMessageService.FullTimeDrawMessage(finalResult);
            return true;
        }

        message = string.Empty;
        return false;
    }


    public static bool IsFinalOfBo2InValid(ResultDetail finalResult, ResultDetail halfTimeResult, IEnumerable<ResultDetail> results, out string message)
    {
        var isHtSettled = results.Any(r => r.ResourceId == ResourceId.Halftime);
        if (IsFullTimeDraw(finalResult, out message))
        {
            return true;
        }

        if (halfTimeResult is null && !isHtSettled)
        {
            message = CreateMessageService.HalfTimeNotCalculated(finalResult);
            return true;
        }

        if (int.Parse(halfTimeResult!.HomeValue) > int.Parse(finalResult.HomeValue) ||
            int.Parse(halfTimeResult!.AwayValue) > int.Parse(finalResult.AwayValue))
        {
            message = CreateMessageService.HalfTimeInconsistentWithFT(finalResult, halfTimeResult);
            return true;
        }

        message = string.Empty;
        return false;
    }

    public static bool IsFinalOfBo4InValid(ResultDetail finalResult, List<ResultDetail> results, out string message)
    {
        var resultsValid = ResourceId.ResultFtBasketBall.Where(r => results.FirstOrDefault(rd => rd.ResourceId == r) != null).ToArray();

        if (IsFullTimeDraw(finalResult, out message))
        {
            return true;
        }

        if (resultsValid.Length != ResourceId.ResultFtBasketBall.Length)
        {
            message = CreateMessageService.RelatedResultInvalidForFT(finalResult, resultsValid);
            return true;
        }

        if (FullTimeRelatedResultInvalid(finalResult, results, out message))
        {
            return true;
        }

        message = string.Empty;
        return false;
    }


    private static bool FullTimeRelatedResultInvalid(ResultDetail finalResult, List<ResultDetail> results, out string message)
    {

        var totalHomeValue = SumHomeValueFullTime(results);
        var totalAwayValue = SumAwayValueFullTime(results);
        if (IsOtInValid(results, totalHomeValue, totalAwayValue) || IsTotalScoreInValid(finalResult, totalHomeValue, totalAwayValue))
        {
            message = CreateMessageService.RelatedResultScoreInconsistentWithFT(finalResult, results);
            return true;
        }

        message = string.Empty;
        return false;
    }


    private static bool IsOtInValid(IEnumerable<ResultDetail> results, int totalHomeValue, int totalAwayValue)
    {
        var otResult = results.FirstOrDefault(r => r.ResourceId == ResourceId.OverTime);
        var isOtExist = otResult != null && string.CompareOrdinal($"{otResult.HomeValue}:{otResult.AwayValue}", "0:0") != 0;

        if (isOtExist && (totalHomeValue != totalAwayValue || int.Parse(otResult.HomeValue) == int.Parse(otResult.AwayValue)))
        {
            return true;
        }

        return !isOtExist && totalHomeValue == totalAwayValue;
    }

    private static bool IsTotalScoreInValid(ResultDetail finalResult, int totalHomeValue, int totalAwayValue)
    {
        return totalHomeValue != int.Parse(finalResult.HomeValue) || totalAwayValue != int.Parse(finalResult.AwayValue);
    }

    private static int SumHomeValueFullTime(List<ResultDetail> results)
    {
        return results
            .Where(r => r.ResourceId != ResourceId.OverTime && r.ResourceId != ResourceId.Halftime)
            .Select(r => int.Parse(r.HomeValue)).Sum();
    }

    private static int SumAwayValueFullTime(List<ResultDetail> results)
    {
        return results
            .Where(r => r.ResourceId != ResourceId.OverTime && r.ResourceId != ResourceId.Halftime)
            .Select(r => int.Parse(r.AwayValue)).Sum();
    }

    private static bool HalfTimeRelatedResultInValid(ResultDetail halfTimeResult, IEnumerable<ResultDetail> results, out string message)
    {
        var resultsValid = ResourceId.ResultHtBasketBall.Where(r => results.FirstOrDefault(rd => rd.ResourceId == r) != null).ToArray();

        if (resultsValid.Length != ResourceId.ResultHtBasketBall.Length)
        {
            message = CreateMessageService.HalfTimeResultRelatedNotValid(halfTimeResult, resultsValid);
            return true;
        }

        message = string.Empty;
        return false;
    }

    private static bool IsHalfTimeScoreInValid(ResultDetail halfTimeResult, IEnumerable<ResultDetail> results, out string message)
    {
        var resultRelated = results.Where(r => ResourceId.ResultHtBasketBall.Contains(r.ResourceId)).ToList();

        var home = resultRelated.Select(r => int.Parse(r.HomeValue)).Sum();
        var away = resultRelated.Select(r => int.Parse(r.AwayValue)).Sum();

        if (home != int.Parse(halfTimeResult.HomeValue) || away != int.Parse(halfTimeResult.AwayValue))
        {
            message = CreateMessageService.ResultRelatedInconsistentWithHT(halfTimeResult, resultRelated);
            return true;
        }

        message = string.Empty;
        return false;
    }
}
