using AutoPayout.Conditions.Services.HelperSerivce;

namespace AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;
public static partial class VerifyFinalResultHelper
{
    public static bool IsHalfTimeInValidForBasketball(ResultDetail halfTimeResult, List<ResultDetail> results, int gameSession, out string message)
    {
        if (gameSession == Bo1)
        {
            message = CreateMessageService.HalfTimeInconsistentWithGameSessionBB(halfTimeResult, gameSession);
            return true;
        }

        if (AreResultsInValidForBasketball(results, out message))
        {
            return true;
        }

        if (IsHalfTimeScoreInValid(halfTimeResult, results, out message))
        {
            return true;
        }

        message = string.Empty;
        return false;
    }


    public static bool IsFinalInValidForBasketball(ResultDetail finalResult, List<ResultDetail> results, int gameSession, out string message)
    {



        message = string.Empty;
        return false;
    }

    private static bool AreResultsInValidForBasketball(IEnumerable<ResultDetail> results, out string message)
    {
        var resultsValid = ResourceId.ResultHtBasketBall.Where(r => results.FirstOrDefault(rd => rd.ResourceId == r, null) != null).ToArray();

        if (resultsValid.Length != ResourceId.ResultHtBasketBall.Length)
        {
            message = CreateMessageService.HalfTimeResultRelatedNotValidBB(null, resultsValid);
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
            message = CreateMessageService.HalfTimeScoreNotEqualWithResultRelatedBB(halfTimeResult, resultRelated);
            return true;
        }

        message = string.Empty;
        return false;
    }
}
