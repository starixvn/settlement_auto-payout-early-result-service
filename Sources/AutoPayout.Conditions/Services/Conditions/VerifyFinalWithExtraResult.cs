using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using AutoPayout.Helpers;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyFinalWithExtraResult : ICondition
{
	private readonly ResultDetailService resultDetailService;
	private readonly IDynamicRepository repository;
	private readonly IOptions<AppSettingsKey> appSettingsKey;
	private readonly ExtraResultService extraResultService;

	public VerifyFinalWithExtraResult(
		ResultDetailService resultDetailService,
		IDynamicRepository repository,
		IOptions<AppSettingsKey> options,
		ExtraResultService extraResultService)
	{
		this.resultDetailService = resultDetailService;
		this.repository = repository;
		this.appSettingsKey = options;
		this.extraResultService = extraResultService;
	}

	public async Task HandleCondition(Match match)
	{
		var finalResult = match.ResultDetails.Find(i => i.ResourceId == ResourceIds.FullTime);

		var matchDetail = await repository.GetAsync<MatchDetail>(new GetMatchDetailCriteria { MatchId = match.MatchId });

		var notCheckFinalResult = match.ResourceType != ResourceType.MainResource
			|| finalResult is null
			|| match.SportType == SportType.BaseBall && (int)matchDetail.SessionTime == 1;

		if (notCheckFinalResult)
		{
			return;
		}

		var settledResults = await resultDetailService.GetResultSettled(match.MatchId, match.SportType);

		var errorMessage = await VerifyFinalResult(match, matchDetail, finalResult, settledResults);

		if (!string.IsNullOrEmpty(errorMessage))
		{
			await HandleInValidFinalResult(match, finalResult, errorMessage);
		}
	}

	public async Task<string> VerifyFinalResult(Match match, MatchDetail matchDetail, ResultDetail finalResult, List<ResultDetail> settledResults)
	{
		var (setResults, resources) = await resultDetailService.GetResultsChecking(match, matchDetail.GameSession, settledResults);

		var validResources = resources.Where(x => !x.Equals(ResourceIds.ExtraInning)).ToList();

		if (validResources.Count != setResults.Count)
		{
			return await FullTimeRelatedResultInValidAndMaxTimeScore(match, finalResult, setResults.Select(x => x.ResourceId), validResources.Select(x => x.ResourceId));
		}

		var isInvalid = await FinalResultScoreInvalid(match, finalResult, setResults);
		if (isInvalid)
		{
			return CreateMessageService.ResultRelatedInconsistent(finalResult, setResults);
		}

		match.ResultDetails.AddIf(setResults.TryGetValue(r => r.ResourceId == ResourceIds.OverTime && r.IsValid, out var overTime), overTime);

		return string.Empty;
	}

	private async Task<bool> FinalResultScoreInvalid(Match match, ResultDetail finalResult, List<ResultDetail> results)
	{
		var totalHomeValue = results.Sum(x => int.Parse(x.HomeValue));
		var totalAwayValue = results.Sum(x => int.Parse(x.AwayValue));

		var extraResult = await extraResultService.GetScoreExtraInning(match, totalHomeValue, totalAwayValue);

		results.Add(extraResult);

		if (extraResult.NeedRetry)
		{
			match.ResultDetails.Remove(finalResult);
			return false;
		}

		var isTotalScoreInValid = resultDetailService.InValidTotalWithExtraScore(finalResult, extraResult, totalHomeValue, totalAwayValue);

		return !extraResult.IsValid || isTotalScoreInValid;
	}


	private async Task HandleInValidFinalResult(Match match, ResultDetail finalResult, string errorMessage)
	{
		await resultDetailService.UpdateResultStatus(match.MatchId, new List<ResultDetail> { finalResult }, errorMessage);
		resultDetailService.WriteLogResult(match.MatchId, finalResult, errorMessage);
		match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != ResourceIds.FullTime).ToList();
	}

	private async Task<string> FullTimeRelatedResultInValidAndMaxTimeScore(Match match, ResultDetail finalResult, IEnumerable<string> resultsValid, IEnumerable<string> validResources)
	{
		var maxTimeScore = await resultDetailService.MaxTimeScore(finalResult);
		if (maxTimeScore > appSettingsKey.Value.RetryProcessScore)
		{
			return CreateMessageService.RelatedResultInvalid(finalResult, resultsValid, validResources);
		}

		match.ResultDetails.Remove(finalResult);

		return string.Empty;
	}
}
