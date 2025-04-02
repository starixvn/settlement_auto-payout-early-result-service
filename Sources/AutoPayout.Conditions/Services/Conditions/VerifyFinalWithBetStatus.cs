using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyFinalWithBetStatus : ICondition
{
    private const int numberOfSets = 3;
    private readonly ResultDetailService resultDetailService;
    private readonly WriteLogService writeLogService;

    public VerifyFinalWithBetStatus(ResultDetailService resultDetailService, WriteLogService writeLogService)
    {
        this.resultDetailService = resultDetailService;
        this.writeLogService = writeLogService;
    }
    public async Task HandleCondition(Match match)
    {
        if (match.ResourceType != ResourceType.MainResource)
        {
            return;
        }

        var finalResult = match.ResultDetails.Find(i => i.ResourceId == ResourceIds.FullTime);
        var betStatus = match.MatchDetail.BetStatus;

        if (!IsValidBetStatus(betStatus))
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


    private bool IsValidBetStatus(string betStatus)
    {
        if (string.IsNullOrEmpty(betStatus))
        {
            return false;
        }

        var betStatusArray = betStatus.Split(',');
        return betStatusArray.Length >= numberOfSets;
    }

    private List<BetStatus> ParseBetStatusList(string betStatus)
    {
        var betStatusArray = betStatus.Split(',');
        return betStatusArray[..numberOfSets]
            .Select(value => new BetStatus(value.Split('-')[0], value.Split('-')[1]))
            .ToList();
    }

    private (int homeScore, int awayScore) CalculateScores(List<BetStatus> betStatusList)
    {
        var homeScore = betStatusList.Sum(value => int.Parse(value.Home));
        var awayScore = betStatusList.Sum(value => int.Parse(value.Away));
        return (homeScore, awayScore);
    }

    private bool IsValidScores((int homeScore, int awayScore) scores, ResultDetail finalResult)
    {
        return scores.homeScore == int.Parse(finalResult.HomeValue) &&
               scores.awayScore == int.Parse(finalResult.AwayValue);
    }

    private async Task HandleInvalidResult(Match match, ResultDetail finalResult, string errorMessage)
    {
        await resultDetailService.UpdateInvalidResultStatus(match.MatchId, new List<ResultDetail> { finalResult }, errorMessage);

        resultDetailService.WriteLogResult(match.MatchId, finalResult, errorMessage);

        match.ResultDetails.RemoveAll(r => r.ResourceId == ResourceIds.FullTime);
    }

    private sealed record BetStatus(string Home, string Away);
}

