using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyFinalWithBetStatus : AbstractCondition
{
    private const int numberOfSets = 3;
    private const int numberOfSetsWithOt = 4;
    private readonly ResultDetailService resultDetailService;
    private readonly int[] ChildMatchTypeCheck = [18, 19, 20, 22, 23, 24];

    public VerifyFinalWithBetStatus(ResultDetailService resultDetailService)
    {
        this.resultDetailService = resultDetailService;
    }


    private static bool IsValidBetStatus(Match match)
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
        return [.. betStatusArray.Select(value => value.Contains('-')
            ? new BetStatus(value.Split('-')[0], value.Split('-')[1])
            : new BetStatus("0","0"))
        ];
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
        var matchDetail = match.MatchDetail;
        var finalResult = match.ResultDetails.Find(i => i.ResourceId == ResourceIds.FullTime);
        var betStatus = matchDetail.BetStatus;

        if (match.ResourceType != ResourceType.MainResource || finalResult == null)
        {
            return;
        }

        // regular only or child match include OT
        if (IsMatchRegularTimeOnly(matchDetail) || IsChildMatchIncludeOT(matchDetail))
        {
            if (!IsValidBetStatus(match))
            {
                await HandleInvalidResult(match, finalResult, ErrorMessage.InconsistentInterval);
                return;
            }

            var sets = GetNumberOfSets(matchDetail);
            await CheckScoreWithBetStatus(match, finalResult, betStatus, sets);
            return;
        }

        // other matches
        if (matchDetail.ParentID != 0 && !ChildMatchTypeCheck.Contains(matchDetail.ChildMatchType))
        {
            if (!string.IsNullOrEmpty(matchDetail.BetStatus))
            {
                await HandleInvalidResult(match, finalResult, ErrorMessage.InconsistentInterval);
            }
            return;
        }

        // 1p 2p 3p
        await CheckScoreWithBetStatus(match, finalResult, betStatus);

    }

    private async Task CheckScoreWithBetStatus(Match match, ResultDetail finalResult, string betStatus, int? numberOfSets = null)
    {
        var betStatusList = ParseBetStatusList(betStatus);

        var validSets = betStatusList.Take(numberOfSets ?? betStatusList.Count).ToList();
        var scores = CalculateScores(validSets);

        if (!IsValidScores(scores, finalResult, match.MatchDetail))
        {
            await HandleInvalidResult(match, finalResult, ErrorMessage.InconsistentInterval);
        }
    }

    private sealed record BetStatus(string Home, string Away);
}

