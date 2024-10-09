using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;
using AutoPayout.Constants;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyBaseballRegularFinalResult : ICondition
{
	private readonly ResultDetailService resultDetailService;
	private readonly IDynamicRepository repository;

	public VerifyBaseballRegularFinalResult(ResultDetailService resultDetailService, IDynamicRepository repository)
	{
		this.resultDetailService = resultDetailService;
		this.repository = repository;
	}

	public async Task HandleCondition(Match match)
	{
		var finalResult = match.ResultDetails.Find(i => i.ResourceId == ResourceIds.FullTime);
		var extraResult = match.ResultDetails.Find(i => i.ResourceId == ResourceIds.ExtraInning);

		var matchDetail = await repository.GetAsync<MatchDetail>(new GetMatchDetailCriteria { MatchId = match.MatchId });

		var notCheckFinalResult = match.ResourceType != ResourceType.MainResource
			|| finalResult is null
			|| match.SportType == SportType.BaseBall && (int)matchDetail.SessionTime == 1;

		if (notCheckFinalResult)
		{
			return;
		}

		var settledResults = await resultDetailService.GetResultSettled(match.MatchId, match.SportType);

		var (setsResults, _) = await resultDetailService.GetResultsChecking(match, matchDetail.GameSession, settledResults);

		var isFinalConsistent = VerifyFinalResultHelper.IsValidSumFinalResult(setsResults, finalResult);
		var isMatchResultsConsecutive = VerifyFinalResultHelper.IsMatchResultsConsecutive(setsResults);
		var isSetResultsRunningOrSettled = VerifyFinalResultHelper.VerifySetResultsRunningOrSettled(settledResults);

		//var isValid = !setsResults.IsNullOrEmpty()
		//	&& isFinalConsistentWithGameSession
		//	&& VerifyFinal(settledResults, setsResults, finalResult, matchDetail.GameSession);

		//if (!isValid)
		//{
		//	await HandleFinalResult(finalResult, match, !isFinalConsistentWithGameSession);
		//}
	}

	private async Task HandleInValidFinalResult(Match match, ResultDetail finalResult)
	{
		var message = "";
		var errorMessage = message.Replace("@score", $"{finalResult.HomeValue}-{finalResult.AwayValue}");

		await resultDetailService.UpdateResultStatus(match.MatchId, new List<ResultDetail> { finalResult }, errorMessage);
		resultDetailService.WriteLogResult(match.MatchId, finalResult, errorMessage);
		match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != ResourceIds.FullTime).ToList();
	}
}
