using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyFinalWithBetStatus : AbstractCondition
{
    private const int numberOfSets = 3;
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

        return betStatusArray.Length >= numberOfSets;
    }

    private static List<BetStatus> ParseBetStatusList(string betStatus)
    {
        var betStatusArray = betStatus.Split(',');
        return [.. betStatusArray[..numberOfSets].Select(value => value.Contains('-')
            ? new BetStatus(value.Split('-')[0], value.Split('-')[1])
            : new BetStatus("0","0"))
        ];
    }

    private static int GetNumberOfSets(MatchDetail matchDetail, List<BetStatus> betStatusList)
    {
        if (matchDetail.ParentID == 0 || matchDetail.ParentID != 0 && matchDetail.LeagueType == 1)
        {
            return numberOfSets;
        }

        return betStatusList.Count;
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

        if (matchDetail.ChildMatchType == 23 && finalHomeScore == finalAwayScore)
        {
            return false;
        }

        return true;
    }


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
        var matchDetail = match.MatchDetail;
        var betStatus = matchDetail.BetStatus;

        if (!IsValidBetStatus(match))
        {
            await HandleInvalidResult(match, finalResult, ErrorMessage.InconsistentInterval);
            return;
        }

        var betStatusList = ParseBetStatusList(betStatus);
        var sets = GetNumberOfSets(matchDetail, betStatusList);
        var scores = CalculateScores([.. betStatusList.Take(sets)]);

        if (!IsValidScores(scores, finalResult, match.MatchDetail))
        {
            await HandleInvalidResult(match, finalResult, ErrorMessage.InconsistentInterval);
        }
    }

    private sealed record BetStatus(string Home, string Away);
}

