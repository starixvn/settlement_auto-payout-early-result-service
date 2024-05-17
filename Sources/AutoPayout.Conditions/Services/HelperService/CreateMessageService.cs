namespace AutoPayout.Conditions.Services.HelperSerivce;
public static class CreateMessageService
{

    public static string HalfTimeInconsistentWithGameSession(ResultDetail halfTimeResult, int gameSession)
    {
        return ErrorMessage.HalfTimeInconsistentWithGameSession
            .Replace("{0}", $"{halfTimeResult.HomeValue}-{halfTimeResult.AwayValue}")
            .Replace("{1}", gameSession.ToString());
    }

    public static string HalfTimeResultRelatedNotValid(ResultDetail halfTimeResult, string[] resultRelatedValid)
    {
        var resultNotValid = ResourceIds.ResultHtBasketBall.Except(resultRelatedValid);
        return ErrorMessage.RelatedResultNotCalculated
            .Replace("{0}", $"HT({halfTimeResult.HomeValue}-{halfTimeResult.AwayValue})")
            .Replace("{1}", string.Join(",", resultNotValid));
    }

    public static string ResultRelatedInconsistentWithHT(ResultDetail halfTimeResult, List<ResultDetail> resultRelatedValid)
    {
        var results = resultRelatedValid.Select(r => $"{r.ResourceId}({r.HomeValue}-{r.AwayValue})");
        return ErrorMessage.RelatedResultInconsistent
            .Replace("{0}", $"HT({halfTimeResult.HomeValue}-{halfTimeResult.AwayValue})")
            .Replace("{1}", string.Join(",", results));
    }

    public static string HalfTimeNotCalculated(ResultDetail fullTimeResult)
    {
        return ErrorMessage.RelatedResultNotCalculated
            .Replace("{0}", $"{fullTimeResult.HomeValue}-{fullTimeResult.AwayValue}")
            .Replace("{1}", "HT");
    }

    public static string HalfTimeInconsistentWithFT(ResultDetail fullTimeResult, ResultDetail halfTime)
    {
        return ErrorMessage.RelatedResultInconsistent
            .Replace("{0}", $"FT({fullTimeResult.HomeValue}-{fullTimeResult.AwayValue})")
            .Replace("{1}", $"HT({halfTime.HomeValue}-{halfTime.AwayValue})");
    }

    public static string RelatedResultInvalidForFT(ResultDetail fullTimeResult, string[] resultRelatedValid)
    {
        var resultNotValid = ResourceIds.ResultHtBasketBall.Except(resultRelatedValid);
        return ErrorMessage.RelatedResultNotCalculated
            .Replace("{0}", $"FT({fullTimeResult.HomeValue}-{fullTimeResult.AwayValue})")
            .Replace("{1}", string.Join(",", resultNotValid).Replace("01", "HT"));
    }

    public static string RelatedResultScoreInconsistentWithFT(ResultDetail fullTimeResult, List<ResultDetail> resultRelatedValid)
    {
        var results = resultRelatedValid.Select(r => $"{r.ResourceId.Replace("01", "HT")}({r.HomeValue}-{r.AwayValue})");
        return ErrorMessage.RelatedResultInconsistent
            .Replace("{0}", $"FT({fullTimeResult.HomeValue}-{fullTimeResult.AwayValue})")
            .Replace("{1}", string.Join(",", results));
    }

    public static string FullTimeDrawMessage(ResultDetail fullTimeResult)
    {
        return ErrorMessage.FullTimeDraw
            .Replace("{0}", $"FT({fullTimeResult.HomeValue}-{fullTimeResult.AwayValue})");
    }
}
