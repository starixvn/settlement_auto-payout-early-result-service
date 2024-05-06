namespace AutoPayout.Conditions.Services.HelperSerivce;
public static class CreateMessageService
{

    public static string HalfTimeInconsistentWithGameSessionBB(ResultDetail halfTimeResult, int gameSession)
    {
        return ErrorMessage.HalfTimeInconsistentWithGameSession
            .Replace("{0}", $"{halfTimeResult.HomeValue}-{halfTimeResult.AwayValue}")
            .Replace("{1}", gameSession.ToString());
    }

    public static string HalfTimeResultRelatedNotValidBB(ResultDetail halfTimeResult, string[] resultRelatedValid)
    {
        var resultNotValid = ResourceId.ResultHtBasketBall.Except(resultRelatedValid);
        return ErrorMessage.HalfTimeInconsistentWithGameSession
            .Replace("{0}", $"{halfTimeResult.HomeValue}-{halfTimeResult.AwayValue}")
            .Replace("{1}", string.Join(",", resultNotValid));
    }

    public static string HalfTimeScoreNotEqualWithResultRelatedBB(ResultDetail halfTimeResult, List<ResultDetail> resultRelatedValid)
    {
        var results = resultRelatedValid.Select(r => $"{r.ResourceId}({r.HomeValue}-{r.AwayValue})");
        return ErrorMessage.HalfTimeInconsistentWithGameSession
            .Replace("{0}", $"{halfTimeResult.HomeValue}-{halfTimeResult.AwayValue}")
            .Replace("{1}", string.Join(",", results));
    }
}
