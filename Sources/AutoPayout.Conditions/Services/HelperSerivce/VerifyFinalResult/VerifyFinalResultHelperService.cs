namespace AutoPayout.Conditions.Services.HelperSerivce.VerifyFinalResult;

public static class VerifyFinalResultHelper
{
    public static bool VerifyResult(IReadOnlyCollection<ResultDetail> setsResults, ResultDetail final)
    {
        var sumHomeScore = setsResults.Count(set => Convert.ToInt32(set.HomeValue) > Convert.ToInt32(set.AwayValue)).ToString();
        var sumAwayScore = setsResults.Count(set => Convert.ToInt32(set.HomeValue) < Convert.ToInt32(set.AwayValue)).ToString();

        return final.HomeValue == sumHomeScore && final.AwayValue == sumAwayScore;
    }

    public static bool IsMatchResultsConsecutive(IEnumerable<ResultDetail> setsResults)
    {
        return !setsResults.Select((i, j) => Convert.ToInt32(i.ResourceId) - j).Distinct().Skip(1).Any();
    }

    public static bool FinalScoreIsValid(MatchDetail matchDetail, ResultDetail finalResult)
    {
        var minSetToWin = (matchDetail.GameSession + 1) / 2;
        var homeValue = Convert.ToInt32(finalResult.HomeValue);
        var awayValue = Convert.ToInt32(finalResult.AwayValue);

        return (homeValue == minSetToWin || awayValue == minSetToWin)
              && (homeValue + awayValue) <= matchDetail.GameSession;
    }
}
