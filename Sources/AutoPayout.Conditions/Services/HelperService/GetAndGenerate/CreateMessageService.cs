using AutoPayout.Constants;
using AutoPayout.Helpers;

namespace AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
public static class CreateMessageService
{
	public static string InconsistentWithGameSession(ResultDetail resultDetail, int gameSession)
	{
		var resourceId = resultDetail.ResourceId.Replace(ResourceIds.FullTime, "FT")
			.Replace(ResourceIds.Halftime, "HT");
		return ErrorMessage.InvalidMatchType
			.Replace("{0}", $"{resourceId}({resultDetail.HomeValue}-{resultDetail.AwayValue})")
			.Replace("{1}", gameSession.ToString());
	}

	public static string ResultsInconsistentWithGameSession(IEnumerable<ResultDetail> resultDetails, int gameSession)
	{
		var results = resultDetails.Select(r => $"{r.ResourceId}({r.HomeValue}-{r.AwayValue})");
		return ErrorMessage.InvalidMatchType
			.Replace("{0}", string.Join(", ", results))
			.Replace("{1}", gameSession.ToString());
	}

	public static string ResultRelatedInconsistent(ResultDetail resultDetail, IReadOnlyCollection<ResultDetail> resultRelatedValid, string extraResource, bool isHalfTime = true)
	{
		var resourceId = resultDetail.ResourceId.Replace(ResourceIds.FullTime, "FT").Replace(ResourceIds.Halftime, "HT");

		var extraResult = resultRelatedValid.FirstOrDefault(r => r.ResourceId == extraResource);

		var results = resultRelatedValid
			.Where(r => r.ResourceId != extraResource)
			.Select(r => $"{(isHalfTime ? r.ResourceId.Replace(ResourceIds.Halftime, "HT") : r.ResourceId)}({r.HomeValue}-{r.AwayValue})").ToList();

		results.AddIf(extraResult != null && !string.IsNullOrEmpty(extraResult.RelatedInfo), $"{extraResource}({extraResult?.RelatedInfo})");

		return ErrorMessage.RelatedResultInconsistent
			.Replace("{0}", $"{resourceId}({resultDetail.HomeValue}-{resultDetail.AwayValue})")
			.Replace("{1}", string.Join(", ", results));
	}

	public static string HalfTimeNotCalculated(ResultDetail fullTimeResult)
	{
		return ErrorMessage.RelatedResultNotCalculated
			.Replace("{0}", $"FT({fullTimeResult.HomeValue}-{fullTimeResult.AwayValue})")
			.Replace("{1}", "HT");
	}

	public static string HalfTimeInconsistentWithFT(ResultDetail fullTimeResult, ResultDetail halfTime)
	{
		return ErrorMessage.RelatedResultInconsistent
			.Replace("{0}", $"FT({fullTimeResult.HomeValue}-{fullTimeResult.AwayValue})")
			.Replace("{1}", $"HT({halfTime.HomeValue}-{halfTime.AwayValue})");
	}

	public static string RelatedResultInvalid(
		ResultDetail resultDetail,
		IEnumerable<string> resultRelatedValid,
		IEnumerable<string> listResourceNeeded,
		bool isHalfTime = true)
	{
		var resourceId = resultDetail.ResourceId
			.Replace(ResourceIds.FullTime, "FT")
			.Replace(ResourceIds.Halftime, "HT");

		var resultNotValid = listResourceNeeded.Except(resultRelatedValid);

		var message = ErrorMessage.RelatedResultNotCalculated
			.Replace("{0}", $"{resourceId}({resultDetail.HomeValue}-{resultDetail.AwayValue})")
			.Replace("{1}", string.Join(", ", resultNotValid));

		return isHalfTime ? message.Replace("01", "HT") : message;

	}

	public static string FullTimeDrawMessage(ResultDetail fullTimeResult)
	{
		return ErrorMessage.FullTimeDraw
			.Replace("{0}", $"FT({fullTimeResult.HomeValue}-{fullTimeResult.AwayValue})");
	}
}
