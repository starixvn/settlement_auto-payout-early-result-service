using AutoPayout.Conditions.Services.HelperSerivce.VerifyFinalResult;

namespace AutoPayout.Conditions.Services;
public class VerifyFinalResult(IDynamicRepository repository, ResultDetailService resultDetailService) : ICondition
{
    public async Task HandleCondition(Match match)
    {
        var finalResult = match.ResultDetails.FirstOrDefault(i => i.ResourceId == ResourceId.Final);
        if (match.ResourceType != ResourceType.MainResource || finalResult is null)
        {
            return;
        }

        var setsResults = await GetSetResults(match);

        var matchDetail = await repository.GetAsync<MatchDetail>(new GetMatchDetailCriteria { MatchId = match.MatchId });
        var isValid =
            VerifyFinalResultHelper.FinalScoreIsValid(matchDetail, finalResult)
            && VerifyFinalResultHelper.IsMatchResultsConsecutive(setsResults)
            && VerifyFinalResultHelper.VerifyResult(setsResults, finalResult);

        if (!isValid)
        {
            await HandleFinalResult(finalResult, match);
        }
    }

    private async Task<List<ResultDetail>> GetSetResults(Match match)
    {
        var intervalSettled = await GetResultSettled(match);

        var intervalChecking = match.ResultDetails.Where(r => r.ResourceId != ResourceId.Final).ToList();

        var setsResults = intervalSettled.Concat(intervalChecking).ToList();

        var setIndex = await resultDetailService.GetResourceIndex(match.SportType);

        setsResults.ForEach(s => s.IntervalNo = setIndex.First(i => i.ResourceId == s.ResourceId).IntervalNo);

        return setsResults.OrderBy(s => s.IntervalNo).ToList();
    }


    private async Task<List<ResultDetail>> GetResultSettled(Match match)
    {
        var intervalDetails = (await repository.FetchAsync<ResultDetail>(new GetResultDetailCriteria { MatchId = match.MatchId, SportType = match.SportType })).ToList();

        return intervalDetails.Where(r => r.ResourceId != ResourceId.Final).Where(r => r.ResultStatusId is ResultStatusId.ManualCompleted or ResultStatusId.AutoCompleted).ToList();
    }

    private async Task HandleFinalResult(ResultDetail finalResult, Match match) //NOSONAR
    {
        await resultDetailService.UpdateResultStatus(match.MatchId, new List<ResultDetail> { finalResult });
        resultDetailService.WriteLogResult(
            match.MatchId,
            finalResult,
            ErrorMessage.FinalInconsistent.Replace("@score", $"{finalResult.HomeValue}-{finalResult.AwayValue}"));
        match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != ResourceId.Final);
    }
}
