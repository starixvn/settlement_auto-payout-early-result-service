namespace AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;

public static class VerifyFinalResultHelper
{
    private const string ESportsFirstMap = "M1";
    private const string ESportsSecondMap = "M2";

    public static bool VerifyResult(IReadOnlyCollection<ResultDetail> setsResults, ResultDetail final)
    {
        var sumHomeScore = setsResults.Count(set => Convert.ToInt32(set.HomeValue) > Convert.ToInt32(string.IsNullOrEmpty(set.AwayValue) ? "0" : set.AwayValue)).ToString();
        var sumAwayScore = setsResults.Count(set => Convert.ToInt32(set.HomeValue) < Convert.ToInt32(string.IsNullOrEmpty(set.AwayValue) ? "0" : set.AwayValue)).ToString();

        return final.HomeValue == sumHomeScore && final.AwayValue == sumAwayScore;
    }

    public static bool VerifyDrawSetResults(IReadOnlyCollection<ResultDetail> setsResults, int playedGame)
    {
        var hasDraw = setsResults.Any(x => x.HomeValue.Equals(x.AwayValue, StringComparison.OrdinalIgnoreCase));
        var newSetResults = setsResults.Where(x => x.IntervalNo < playedGame).ToList();
        var lastResultNotEqual = setsResults.Any(x => x.IntervalNo == playedGame && !x.HomeValue.Equals(x.AwayValue, StringComparison.OrdinalIgnoreCase));

        var sumHomeScore = newSetResults.Count(set => Convert.ToInt32(set.HomeValue) > Convert.ToInt32(string.IsNullOrEmpty(set.AwayValue) ? "0" : set.AwayValue)).ToString();
        var sumAwayScore = newSetResults.Count(set => Convert.ToInt32(set.HomeValue) < Convert.ToInt32(string.IsNullOrEmpty(set.AwayValue) ? "0" : set.AwayValue)).ToString();

        return hasDraw
            && sumAwayScore == sumHomeScore
            && lastResultNotEqual;
    }

    public static bool IsMatchResultsConsecutive(IEnumerable<ResultDetail> setsResults)
    {
        return !setsResults.Select((i, j) => i.IntervalNo - j).Distinct().Skip(1).Any();
    }

    public static (int, int) GetMinMaxSetPlay(MatchDetail matchDetail, bool isGameSession = true)
    {
        var maxSet = isGameSession ? matchDetail.GameSession : matchDetail.BestOfMap;
        var minSet = (maxSet + 1) / 2;

        return (minSet, maxSet);
    }

    public static bool IsValidFinalResult(List<ResultDetail> results, ResultDetail finalResult, int numberOfSetWin, int maxSetPlay)
    {
        var homeWinCount = 0;
        var awayWinCount = 0;

        foreach (var result in results)
        {
            var homeWin = Convert.ToInt32(result.HomeValue) > Convert.ToInt32(result.AwayValue) ? 1 : 0;
            var awayWin = Convert.ToInt32(result.HomeValue) < Convert.ToInt32(result.AwayValue) ? 1 : 0;

            homeWinCount += homeWin;
            awayWinCount += awayWin;

            if (homeWinCount == numberOfSetWin || awayWinCount == numberOfSetWin)
            {
                break; // NOSONAR
            }
        }

        var finalHomeScore = Convert.ToInt32(finalResult.HomeValue);
        var finalAwayScore = Convert.ToInt32(finalResult.AwayValue);
        var isFinalScoreMatched = finalHomeScore == homeWinCount
                                  && finalAwayScore == awayWinCount;

        return isFinalScoreMatched
               && (finalHomeScore == numberOfSetWin || finalAwayScore == numberOfSetWin)
               && (finalHomeScore + finalAwayScore) <= maxSetPlay;
    }


    public static bool IsValidFinalResultSetWin(List<ResultDetail> results, ResultDetail finalResult, int numberOfSetWin, int maxSetPlay)
    {
        var homeValue = Convert.ToInt32(finalResult.HomeValue);
        var awayValue = Convert.ToInt32(finalResult.AwayValue);

        return (homeValue == numberOfSetWin || awayValue == numberOfSetWin)
              && (homeValue + awayValue) <= maxSetPlay;
    }

    public static bool IsValidSumFinalResult(List<ResultDetail> results, ResultDetail finalResult)
    {
        var totalHomeScore = SumHomeScore(results);
        var totalAwayScore = SumAwayScore(results);

        return Convert.ToInt32(finalResult.HomeValue) == totalHomeScore
               && Convert.ToInt32(finalResult.AwayValue) == totalAwayScore;
    }

    public static bool VerifyFinalBO1(ResultDetail finalResult, List<ResultDetail> results)
    {
        var mapOne = results.Find(r => r.ResourceId == ESportsFirstMap);

        return finalResult.HomeValue != finalResult.AwayValue && mapOne != null && results.Count == 1;
    }

    public static bool VerifyResultSettled(IEnumerable<ResultDetail> settledResults, int playedGame)
    {
        var fakeCalculatedResults = settledResults.Where(x => x.SettlementStatusId == SettlementStatusId.Calculated
            && x.IntervalNo > playedGame).ToList();

        return fakeCalculatedResults.Count == 0;
    }

    public static bool VerifySetResultsRunningOrSettled(List<ResultDetail> setResults)
        => setResults.TrueForAll(x => (new[] { SettlementStatusId.Calculated, SettlementStatusId.Running }).Contains(x.SettlementStatusId));

    public static bool VerifyAnyDrawResults(List<ResultDetail> resultsChecking)
        => !resultsChecking.Exists(x => (new[] { SettlementStatusId.Calculated, SettlementStatusId.Running }).Contains(x.SettlementStatusId)
                && x.HomeValue.Equals(x.AwayValue, StringComparison.OrdinalIgnoreCase));

    public static bool VerifyFinalBestOfTWO(ResultDetail finalResult, List<ResultDetail> results)
    {
        var mapOne = results.Find(r => r.ResourceId == ESportsFirstMap);
        var mapTwo = results.Find(r => r.ResourceId == ESportsSecondMap);
        var totalScore = Convert.ToInt32(finalResult.HomeValue) + Convert.ToInt32(finalResult.AwayValue);

        return mapOne != null && mapTwo != null && totalScore == GameType.Bo2;
    }

    public static int SumHomeScore(List<ResultDetail> results)
        => results.Sum(set => Convert.ToInt32(set.HomeValue));

    public static int SumAwayScore(List<ResultDetail> results)
        => results.Sum(set => Convert.ToInt32(set.AwayValue));
}