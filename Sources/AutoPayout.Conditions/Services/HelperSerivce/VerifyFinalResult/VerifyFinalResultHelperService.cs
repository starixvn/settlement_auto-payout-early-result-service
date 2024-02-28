namespace AutoPayout.Conditions.Services.HelperSerivce.VerifyFinalResult;

public static class VerifyFinalResultHelper
{
    private static readonly string ESportsFirstMap = "M1";
    private static readonly string ESportsSecondMap = "M2";

    public static bool VerifyResult(IReadOnlyCollection<ResultDetail> setsResults, ResultDetail final)
    {
        var sumHomeScore = setsResults.Count(set => Convert.ToInt32(set.HomeValue) > Convert.ToInt32(set.AwayValue)).ToString();
        var sumAwayScore = setsResults.Count(set => Convert.ToInt32(set.HomeValue) < Convert.ToInt32(set.AwayValue)).ToString();

        return final.HomeValue == sumHomeScore && final.AwayValue == sumAwayScore;
    }

    public static bool IsMatchResultsConsecutive(IEnumerable<ResultDetail> setsResults)
    {
        return !setsResults.Select((i, j) => i.IntervalNo - j).Distinct().Skip(1).Any();
    }

    public static bool FinalScoreIsValid(MatchDetail matchDetail, ResultDetail finalResult, bool isGameSession = true)
    {
        var minSetToWin = (matchDetail.GameSession + 1) / 2;
        var homeValue = Convert.ToInt32(finalResult.HomeValue);
        var awayValue = Convert.ToInt32(finalResult.AwayValue);
        var maxSet = isGameSession ? matchDetail.GameSession : matchDetail.BestOfMatch;

        return (homeValue == minSetToWin || awayValue == minSetToWin)
              && (homeValue + awayValue) <= maxSet;
    }

    public static bool VerifyFinalBO1(ResultDetail finalResult, List<ResultDetail> results)
    {
        var mapOne = results.FirstOrDefault(r => r.ResourceId == ESportsFirstMap);

        return finalResult.HomeValue != finalResult.AwayValue && mapOne != null && results.Count() == 1;
    }

    public static bool VerifyFinalBestOfTWO(ResultDetail finalResult, List<ResultDetail> results)
    {
        var mapOne = results.FirstOrDefault(r => r.ResourceId == ESportsFirstMap);
        var mapTwo = results.FirstOrDefault(r => r.ResourceId == ESportsSecondMap);
        var totalScore = Convert.ToInt32(finalResult.HomeValue) + Convert.ToInt32(finalResult.AwayValue);

        return mapOne != null && mapTwo != null && totalScore == 2;
    }
}
