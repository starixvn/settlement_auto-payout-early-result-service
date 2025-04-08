using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyFinalWithBetStatus : AbstractCondition
{
    private const int numberOfSets = 3;
    private const int numberOfSetsWithOt = 4;
    private readonly ResultDetailService resultDetailService;
    private readonly int[] ChildMatchTypeCheck = [18, 19, 20, 23, 24];

    public VerifyFinalWithBetStatus(ResultDetailService resultDetailService)
    {
        this.resultDetailService = resultDetailService;
    }


    private bool IsValidBetStatus(Match match)
    {
        if (string.IsNullOrEmpty(match.MatchDetail.BetStatus))
        {
            return false;
        }

        var betStatusArray = match.MatchDetail.BetStatus.Split(',');
        var numSets = GetNumberOfSets(match.MatchDetail);

        return betStatusArray.Length >= numSets;
    }

    private static List<BetStatus> ParseBetStatusList(string betStatus)
    {
        var betStatusArray = betStatus.Split(',');
        return [.. betStatusArray[..numberOfSets].Select(value => new BetStatus(value.Split('-')[0], value.Split('-')[1]))];
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

    private static bool IsValidScores((int homeScore, int awayScore) scores, ResultDetail finalResult)
        => scores.homeScore == int.Parse(finalResult.HomeValue) &&
               scores.awayScore == int.Parse(finalResult.AwayValue);

    private async Task HandleInvalidResult(Match match, ResultDetail finalResult, string errorMessage)
    {
        await resultDetailService.UpdateInvalidResultStatus(match.MatchId, [finalResult], errorMessage);

        resultDetailService.WriteLogResult(match.MatchId, finalResult, errorMessage);

        match.ResultDetails.RemoveAll(r => r.ResourceId == ResourceIds.FullTime);
    }

    public override async Task HandleCondition(Match match)
    {
        if (match.ResourceType != ResourceType.MainResource
            || (match.MatchDetail.ParentID != 0 && !ChildMatchTypeCheck.Contains(match.MatchDetail.ChildMatchType)))
        {
            return;
        }

        var finalResult = match.ResultDetails.Find(i => i.ResourceId == ResourceIds.FullTime);
        var betStatus = match.MatchDetail.BetStatus;

        if (!IsValidBetStatus(match))
        {
            await HandleInvalidResult(match, finalResult, ErrorMessage.InconsistentInterval);
            return;
        }

        var betStatusList = ParseBetStatusList(betStatus);
        var scores = CalculateScores(betStatusList);

        if (!IsValidScores(scores, finalResult))
        {
            await HandleInvalidResult(match, finalResult, ErrorMessage.InconsistentInterval);
        }
    }

    private sealed record BetStatus(string Home, string Away);
}

