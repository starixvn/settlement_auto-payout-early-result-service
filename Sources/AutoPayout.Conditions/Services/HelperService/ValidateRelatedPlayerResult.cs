using AutoPayout.Constants;

namespace AutoPayout.Conditions.Services.HelperService;

public static class ValidateRelatedPlayerResult
{
    private static readonly IReadOnlyDictionary<string, Func<ResultDetail, bool>> RuleDispatch
    = new Dictionary<string, Func<ResultDetail, bool>>
    {
        ["669"] = IsHomeValueSumValid,
        ["671"] = IsHomeValueSumValid,
        ["673"] = IsHomeValueSumValid,
        ["674"] = IsHomeValueSumValid,

        ["677"] = IsValidDoubleDouble,
        ["679"] = IsValidTrippleDouble,
        ["675"] = IsValidPerformanceOther,

        ["680"] = IsValidHeadToHead,
        ["681"] = IsValidHeadToHead,
        ["682"] = IsValidHeadToHead,
        ["683"] = IsValidHeadToHead,
        ["684"] = IsValidHeadToHead,
    };

    // 669XX ＠player_name Points-Rebounds-Assist
    // 671XX ＠player _name Rebounds-Assists
    // 673XX ＠player_name Points-Rebounds
    // 674XX ＠player_name Steals-Blocks
    public static bool IsHomeValueSumValid(this ResultDetail resultDetail)
    {
        if (!int.TryParse(resultDetail.HomeValue, out var mainHomeValue))
        {
            return false;
        }

        var relatedSum = resultDetail.RelatedResults
            .Where(related => int.TryParse(related.HomeValue, out _))
            .Sum(related => int.Parse(related.HomeValue));

        return mainHomeValue == relatedSum;
    }


    // 677XX ＠player_name Double Double => Yes
    public static bool IsValidDoubleDouble(this ResultDetail resultDetail)
    {
        const int doubleDoubleThreshold = 10;
        const int requiredStatsForDoubleDouble = 2;
        const int requiredStatsToAvoidDoubleDouble = 4;

        var homeValues = GetValidHomeValues(resultDetail.RelatedResults);
        var statsAboveThreshold = homeValues.Count(value => value >= doubleDoubleThreshold);

        var statsBelowThreshold = homeValues.Count(value => value < doubleDoubleThreshold);

        return resultDetail.HomeValue switch
        {
            "Y" when statsAboveThreshold >= requiredStatsForDoubleDouble => true,
            "N" when statsBelowThreshold >= requiredStatsToAvoidDoubleDouble => true,
            _ => false
        };
    }


    /// <summary>
    /// 677XX ＠player_name Double Double
    /// </summary>
    public static bool IsValidTrippleDouble(this ResultDetail resultDetail)
    {
        const int threshold = 10;
        const int requiredStatsForTrippleDouble = 3;

        var homeValues = GetValidHomeValues(resultDetail.RelatedResults);
        var aboveThreshold = homeValues.Count(value => value >= threshold);


        return resultDetail.HomeValue switch
        {
            "Y" when aboveThreshold >= requiredStatsForTrippleDouble => true,
            "N" when aboveThreshold < requiredStatsForTrippleDouble => true,
            _ => false
        };
    }

    /// <summary>
    ///  
    /// 675XXYY ＠player_name ＠performance and ＠others
    /// </summary>
    public static bool IsValidPerformanceOther(this ResultDetail resultDetail)
    {
        var expectedHomeValue = GetExpectedHomeValue(resultDetail);

        return resultDetail.HomeValue == expectedHomeValue;
    }

    /// <summary>
    /// 680XX Most Points Scored - ＠player_1 vs. ＠player_2
    /// 684XX Most 3-Point Shots Scored - ＠player_1 vs. ＠player_2
    /// 683XX Most Assists - ＠player_1 vs. ＠player_2
    /// 681XX Most Rebounds - ＠player_1 vs. ＠player_2
    /// 682XX Most Steals - ＠player_1 vs. ＠player_2
    /// </summary>
    public static bool IsValidHeadToHead(this ResultDetail resultDetail)
    {
        if (resultDetail.RelatedResults.Count != 2)
        {
            return false;
        }

        var firstRelated = resultDetail.RelatedResults[0];
        var secondRelated = resultDetail.RelatedResults[1];

        if (!int.TryParse(firstRelated.HomeValue, out var firstValue) ||
            !int.TryParse(secondRelated.HomeValue, out var secondValue))
        {
            return false;
        }
        var isFirstWinner = firstValue > secondValue;

        var expectedHomeValue = isFirstWinner
            ? GetBetChoice(firstRelated, resultDetail.OddFlex)
            : GetBetChoice(secondRelated, resultDetail.OddFlex);

        return expectedHomeValue == resultDetail.HomeValue;
    }

    private static string GetBetChoice(ResultDetail relatedResult, List<OddFlexModel> oddFlexes)
    {
        var playerId = string.IsNullOrEmpty(relatedResult.OtherInfo) ? 0 : int.Parse(relatedResult.OtherInfo.Split(",")[0]);
        var oddFlex = oddFlexes.Find(oddflex => oddflex.Info.PlayerId == playerId);

        return oddFlex.BetChoice;
    }


    // winner/handicap/over 675XXYY ＠player_name ＠performance and ＠others
    private static string GetExpectedHomeValue(ResultDetail resultDetail)
    {
        var oddFlex = resultDetail.OddFlex[0];
        var isValidFinalResult = ValidateFinalResult(resultDetail, oddFlex);
        var isValidStatistics = ValidateStatistics(resultDetail, oddFlex);

        if (isValidFinalResult && isValidStatistics)
        {
            return "Y";
        }

        return "N";
    }

    /// <summary>
    /// Validate dựa trên ResourceId pattern
    /// </summary>
    public static bool ValidateByConsistenceRule(this ResultDetail resultDetail)
    {
        var id = resultDetail.ResourceId ?? string.Empty;
        var key = id.Length >= 3 ? id[..3] : id;

        return !RuleDispatch.TryGetValue(key, out var rule) || rule(resultDetail);
    }

    private static bool CompareWithCondition(int value, string condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
        {
            return false;
        }

        condition = condition.Trim();

        if (condition.EndsWith('+'))
        {
            var thresholdStr = condition[..^1]; // Remove "+"
            if (int.TryParse(thresholdStr, out var threshold))
            {
                return value >= threshold;
            }
        }

        if (condition.EndsWith('-'))
        {
            var thresholdStr = condition[..^1]; // Remove "-"
            if (int.TryParse(thresholdStr, out var threshold))
            {
                return value < threshold;
            }
        }

        if (int.TryParse(condition, out var exactValue))
        {
            return value == exactValue;
        }

        return false;
    }

    private static bool ValidateStatistics(ResultDetail resultDetail, OddFlexModel oddFlex)
    {
        var statisticToResourceId = new Dictionary<string, string>
        {
            { "points", "653" },
            { "rebounds", "659"},
            { "assists", "656" },
            { "steals", "667" },
            { "blocks", "665" },
            { "three_points", "662" }
        };

        return oddFlex.Info.Statistics.TrueForAll(statistic =>
        {
            var betType = statisticToResourceId.GetValueOrDefault(statistic.Type);
            if (string.IsNullOrEmpty(betType))
            {
                return true;
            }

            var relatedResult = resultDetail.RelatedResults
                .Find(x => x.ResourceId.StartsWith(betType));

            if (relatedResult == null ||
                !int.TryParse(relatedResult.HomeValue, out var statisticValue))
            {
                return false;
            }

            return CompareWithCondition(statisticValue, statistic.Value);
        });
    }

    private static List<int> GetValidHomeValues(List<ResultDetail> relatedResults)
    {
        return [.. relatedResults
            .Where(related => int.TryParse(related.HomeValue, out _))
            .Select(related => int.Parse(related.HomeValue))];
    }

    private static bool ValidateFinalResult(ResultDetail resultDetail, OddFlexModel oddFlex)
    {
        var hasGameResults = oddFlex.Info.Results
            .Exists(x => x.Type == "winner" || x.Type == "over" || x.Type == "handicap");

        if (!hasGameResults)
        {
            return true;
        }

        var ftResult = resultDetail.RelatedResults
            .Find(x => x.ResourceId == ResourceIds.FullTime);

        if (ftResult == null ||
            !int.TryParse(ftResult.HomeValue, out var ftHomeValue) ||
            !int.TryParse(ftResult.AwayValue, out var ftAwayValue))
        {
            return false;
        }

        return ValidateWinnerCondition(oddFlex, ftHomeValue, ftAwayValue) &&
               ValidateOverCondition(oddFlex, ftHomeValue, ftAwayValue) &&
               ValidateHandicapCondition(oddFlex, ftHomeValue, ftAwayValue);
    }

    private static bool ValidateWinnerCondition(OddFlexModel oddFlex, int ftHomeValue, int ftAwayValue)
    {
        var winnerResult = oddFlex.Info.Results.Find(x => x.Type == "winner");
        if (winnerResult == null)
        {
            return true;
        }

        if (!int.TryParse(winnerResult.Value, out var winnerTeamId))
        {
            return false;
        }

        var isCheckHomeWin = winnerTeamId == oddFlex.Info.TeamId && oddFlex.Info.IsHome;

        return isCheckHomeWin && ftHomeValue > ftAwayValue ||
               !isCheckHomeWin && ftHomeValue < ftAwayValue;
    }

    private static bool ValidateOverCondition(OddFlexModel oddFlex, int ftHomeValue, int ftAwayValue)
    {
        var overResult = oddFlex.Info.Results.Find(x => x.Type == "over");
        if (overResult == null)
        {
            return true;
        }

        if (!double.TryParse(overResult.Value, out var overThreshold))
        {
            return false;
        }

        var totalPoints = ftHomeValue + ftAwayValue;
        return totalPoints > overThreshold;
    }

    private static bool ValidateHandicapCondition(OddFlexModel oddFlex, int ftHomeValue, int ftAwayValue)
    {
        var handicapResult = oddFlex.Info.Results.Find(x => x.Type == "handicap");
        if (handicapResult == null)
        {
            return true;
        }

        if (!double.TryParse(handicapResult.Value, out var handicapValue))
        {
            return false;
        }

        var isCheckHomeWin = oddFlex.Info.IsHome;

        return isCheckHomeWin
            ? ftHomeValue - ftAwayValue > handicapValue
            : ftAwayValue - ftHomeValue > handicapValue;
    }
}
