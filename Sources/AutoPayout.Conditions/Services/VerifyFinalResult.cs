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

        var intervalSettled = await GetResultSettled(match);

        var intervalChecking = match.ResultDetails.Where(r => r.ResourceId != ResourceId.Final).ToList();

        var isValid = VerifyResult(intervalSettled, intervalChecking, finalResult);

        if (!isValid)
        {
            await HandleFinalResult(finalResult, match);
        }
    }


    private async Task<List<ResultDetail>> GetResultSettled(Match match)
    {
        var intervalDetails = (await repository.FetchAsync<ResultDetail>(new GetResultDetailCriteria { MatchId = match.MatchId, SportType = match.SportType })).ToList();

        return intervalDetails.Where(r => r.ResourceId != ResourceId.Final).Where(r => r.ResultStatusId is ResultStatusId.ManualCompleted or ResultStatusId.AutoCompleted).ToList();
    }

    private static bool VerifyResult(IEnumerable<ResultDetail> intervalSettled, IEnumerable<ResultDetail> intervalChecking, ResultDetail final)
    {
        var setsResults = intervalSettled.Concat(intervalChecking).ToList();
        var sumHomeScore = setsResults.Count(set => Convert.ToInt32(set.HomeValue) > Convert.ToInt32(set.AwayValue)).ToString();
        var sumAwayScore = setsResults.Count(set => Convert.ToInt32(set.HomeValue) < Convert.ToInt32(set.AwayValue)).ToString();

        return final.HomeValue == sumHomeScore && final.AwayValue == sumAwayScore;
    }

    private async Task HandleFinalResult(ResultDetail finalResult, Match match)
    {
        await resultDetailService.UpdateResultStatus(match.MatchId, new List<ResultDetail> { finalResult });
        resultDetailService.WriteLogResult(
            match.MatchId,
            finalResult,
            ErrorMessage.FinalInconsistent.Replace("@score", $"{finalResult.HomeValue}-{finalResult.AwayValue}"));
        match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != ResourceId.Final);
    }
}
