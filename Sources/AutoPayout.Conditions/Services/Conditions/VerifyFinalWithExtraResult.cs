using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;
using AutoPayout.Constants;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyFinalWithExtraResult : ICondition
{
	private readonly ResultDetailService resultDetailService;
	private readonly IDynamicRepository repository;
	private readonly IOptions<AppSettingsKey> appSettingsKey;

	public VerifyFinalWithExtraResult(ResultDetailService resultDetailService, IDynamicRepository repository)
	{
		this.resultDetailService = resultDetailService;
		this.repository = repository;
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

		var (setsResults, _) = await resultDetailService.GetResultsChecking(match, matchDetail.GameSession, settledResults);

		var isMatchResultsConsecutive = VerifyFinalResultHelper.IsMatchResultsConsecutive(setsResults);

		var isSetResultsRunningOrSettled = VerifyFinalResultHelper.VerifySetResultsRunningOrSettled(setsResults);

		var isFinalResultScoreInvalid = await FinalResultScoreInvalid(match, finalResult, setsResults);

		if (!isMatchResultsConsecutive || !isSetResultsRunningOrSettled || isFinalResultScoreInvalid)
		{
			await HandleInValidFinalResult(match, finalResult);
		}
	}

	private async Task<bool> FinalResultScoreInvalid(Match match, ResultDetail finalResult, List<ResultDetail> results)
	{
		var totalHomeValue = results.Sum(x => int.Parse(x.HomeValue));
		var totalAwayValue = results.Sum(x => int.Parse(x.AwayValue));

		var extraResult = await GetScoreExtraInning(finalResult.MatchId, finalResult.SportType, totalHomeValue, totalAwayValue);

		results.Add(extraResult);

		if (extraResult.NeedRetry)
		{
			match.ResultDetails.Remove(finalResult);
			return false;
		}

		var isTotalScoreInValid = IsTotalScoreInValid(finalResult, totalHomeValue, totalAwayValue, extraResult);
		return !extraResult.IsValid || isTotalScoreInValid;
	}


	private static bool IsTotalScoreInValid(ResultDetail finalResult, int totalHomeValue, int totalAwayValue, ResultDetail otResult)
	{
		totalHomeValue += otResult == null ? 0 : int.Parse(otResult.HomeValue);
		totalAwayValue += otResult == null ? 0 : int.Parse(otResult.AwayValue);

		return totalHomeValue != int.Parse(finalResult.HomeValue) || totalAwayValue != int.Parse(finalResult.AwayValue);
	}

	private async Task<ResultDetail> GetScoreExtraInning(int matchId, int sportType, int totalHomeValue, int totalAwayValue)
	{
		var result = new ResultDetail
		{
			MatchId = matchId,
			SportType = sportType,
			ResourceId = ResourceIds.ExtraInning,
			HomeValue = "0",
			AwayValue = "0"
		};

		var scoresExtraInning = (await resultDetailService.GetSingleScore(result)).ToList();

		var validScores = scoresExtraInning.Where(x => !string.IsNullOrEmpty(x.HomeValue) && !string.IsNullOrEmpty(x.AwayValue)).ToList();

		result.IsValid = IsOtValid(result, totalHomeValue, totalAwayValue, validScores);
		result.RelatedInfo = string.Join(',', validScores.Select(s => $"{s.SourceName ?? "Trader"}: {s.HomeValue}-{s.AwayValue}"));


		if (result.IsValid && totalAwayValue == totalHomeValue)
		{
			result.HomeValue = validScores[0].HomeValue;
			result.AwayValue = validScores[0].AwayValue;
		}

		return result;
	}


	private bool IsOtValid(ResultDetail extraResult, int totalHomeValue, int totalAwayValue, IReadOnlyList<Score> scores)
	{
		var variantCount = scores.Distinct(new ScoreComparer()).Count();
		if (totalAwayValue != totalHomeValue)
		{
			return variantCount == 0;
		}

		switch (variantCount)
		{
			case > 1:
				{
					var maxDate = scores.Max(t => t.CreateDate);
					var processTime = DateTimeOffset.Now - maxDate;
					extraResult.Expired = processTime.TotalMilliseconds > appSettingsKey.Value.RetryProcessScore;
					extraResult.NeedRetry = !extraResult.Expired;
					return false;
				}
			case 1:
				return int.Parse(scores[0].HomeValue) != int.Parse(scores[0].AwayValue);

			default:
				return false;
		}
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
