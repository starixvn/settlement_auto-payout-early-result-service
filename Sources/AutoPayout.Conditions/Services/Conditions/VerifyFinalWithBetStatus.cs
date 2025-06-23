using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyFinalWithBetStatus(ResultDetailService resultDetailService) : AbstractCondition
{
    private const int numberOfSets = 3;
    private const int numberOfSetsWithOt = 4;
    private readonly int[] ChildMatchTypeCheck = [18, 19, 20, 22, 23, 24];

    private static bool TryParseBetStatus(string betStatus, out List<BetStatus> betStatusList)
    {
        try
        {
            betStatusList = [];
            if (string.IsNullOrEmpty(betStatus))
            {
                return false;
            }

            betStatusList = [.. betStatus.Split(',')
                        .Select(v => v.Split('-') is [var home, var away]
                                     ? new BetStatus(home, away)
                                     : new BetStatus("0", "0"))];
            return true;
        }
        catch
        {
            betStatusList = [];
            return false;
        }
    }

    private static int GetNumberOfSets(MatchDetail matchDetail)
    {
        if (matchDetail.ParentID != 0 && matchDetail.LeagueType == 2)
        {
            return numberOfSetsWithOt;
        }

        return numberOfSets;
    }

    private static (int homeScore, int awayScore) CalculateScores(List<BetStatus> betStatusList)
    {
        var homeScore = betStatusList.Sum(value => int.Parse(value.Home));
        var awayScore = betStatusList.Sum(value => int.Parse(value.Away));
        return (homeScore, awayScore);
    }

    private static bool IsValidScores((int homeScore, int awayScore) scores, ResultDetail finalResult, MatchDetail matchDetail)
    {
        var finalHomeScore = int.Parse(finalResult.HomeValue);
        var finalAwayScore = int.Parse(finalResult.AwayValue);

        if (scores.homeScore != finalHomeScore || scores.awayScore != finalAwayScore)
        {
            return false;
        }

        if (IsChildMatchIncludeOT(matchDetail) && finalHomeScore == finalAwayScore)
        {
            return false;
        }

        return true;
    }

    private static bool IsChildMatchIncludeOT(MatchDetail matchDetail)
        => matchDetail.ParentID != 0 && matchDetail.LeagueType == 2;

    private static bool IsMatchRegularTimeOnly(MatchDetail matchDetail) => matchDetail.LeagueType == 1;

    private async Task HandleInvalidResult(Match match, ResultDetail finalResult, string errorMessage)
    {
        await resultDetailService.UpdateInvalidResultStatus(match.MatchId, [finalResult], errorMessage);

        resultDetailService.WriteLogResult(match.MatchId, finalResult, errorMessage);

        match.ResultDetails.RemoveAll(r => r.ResourceId == ResourceIds.FullTime);
    }

    public override async Task HandleCondition(Match match)
    {
        match.LogCondition.Add($"{ConditionId.VerifyFinalBetStatus} - {GetType().Name}");
        var finalResult = match.ResultDetails.Find(i => i.ResourceId == ResourceIds.FullTime);

        if (match.ResourceType != ResourceType.MainResource || finalResult == null)
        {
            await HandleNextCondition(match);
            return;
        }

        var matchDetail = match.MatchDetail;

        if (IsMatchRegularTimeOnly(matchDetail) || IsChildMatchIncludeOT(matchDetail))
        {
            await HandleRegularOrChildMatchWithOT(match, finalResult);
        }
        else if (IsOtherChildMatch(matchDetail))
        {
            await HandleOtherChildMatch(match, finalResult);
        }
        else
        {
            _ = TryParseBetStatus(matchDetail.BetStatus, out var betStatusList);
            await CheckScoreWithBetStatus(match, finalResult, betStatusList);
        }

        await HandleNextCondition(match);
    }

    private bool IsOtherChildMatch(MatchDetail matchDetail)
        => matchDetail.ParentID != 0 && !ChildMatchTypeCheck.Contains(matchDetail.ChildMatchType);

    private async Task HandleRegularOrChildMatchWithOT(Match match, ResultDetail finalResult)
    {
        var matchDetail = match.MatchDetail;
        var isValidBetStatus = TryParseBetStatus(matchDetail.BetStatus, out var betStatusList);

        if (!isValidBetStatus || betStatusList.Count < numberOfSets || (IsMatchRegularTimeOnly(matchDetail) && HasOt(betStatusList)))
        {
            await HandleInvalidResult(match, finalResult, ErrorMessage.InconsistentInterval);
        }
        else
        {
            var sets = GetNumberOfSets(matchDetail);
            await CheckScoreWithBetStatus(match, finalResult, betStatusList, sets);
        }
    }

    private async Task HandleOtherChildMatch(Match match, ResultDetail finalResult)
    {
        if (!string.IsNullOrEmpty(match.MatchDetail.BetStatus))
        {
            await HandleInvalidResult(match, finalResult, ErrorMessage.InconsistentInterval);
        }
    }

    private async Task CheckScoreWithBetStatus(Match match, ResultDetail finalResult, List<BetStatus> betStatusList, int? numberOfSets = null)
    {
        var validSets = betStatusList.Take(numberOfSets ?? betStatusList.Count).ToList();
        var scores = CalculateScores(validSets);

        if (!IsValidScores(scores, finalResult, match.MatchDetail))
        {
            await HandleInvalidResult(match, finalResult, ErrorMessage.InconsistentInterval);
        }
    }

    private static bool HasOt(List<BetStatus> betStatusList)
    {
        if (betStatusList.Count != numberOfSetsWithOt)
        {
            return false;
        }

        var otSetIsEmpty = string.IsNullOrEmpty(betStatusList[3].Home) && string.IsNullOrEmpty(betStatusList[3].Away);
        var thirdSetIsScored = betStatusList[2].Home != "0" && betStatusList[2].Away != "0";

        return otSetIsEmpty && thirdSetIsScored;
    }

    private sealed record BetStatus(string Home, string Away);
}