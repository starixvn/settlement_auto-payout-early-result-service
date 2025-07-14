using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Constants;
using Microsoft.Extensions.Options;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyFinalKabaddi(ResultDetailService resultDetailService, IOptions<AppSettingsKey> options) : AbstractCondition
{
    private const byte TossWinner = 2;
    private const byte TeamTotalPoints = 1;
    private const byte RaidPoints = 3;
    private const byte PlayerRaidPoints = 4;
    private const int TeamIdNotAvailable = 18486;

    public override async Task HandleCondition(Match match)
    {
        var matchDetail = match.MatchDetail;
        var finalResult = match.ResultDetails.Find(i => i.ResourceId == ResourceIds.FullTime);

        var (homeFtScore, awayFtScore) = ParseScores(finalResult);

        if (matchDetail.ChildMatchType == TossWinner)
        {
            ValidateTossWinner(finalResult, homeFtScore, awayFtScore);
        }
        else if (IsTeamTotalOrRaidPoints(matchDetail.ChildMatchType))
        {
            ValidateTeamTotalOrRaidPoints(matchDetail, finalResult, homeFtScore, awayFtScore);
        }

        if (!finalResult.IsValid)
        {
            await HandleFinalResult(finalResult, match);
        }

        await HandleNextCondition(match);
    }

    private static void ValidateTeamTotalOrRaidPoints(MatchDetail matchDetail, ResultDetail finalResult, int homeScore, int awayScore)
    {
        var isHomeIdNotAvailable = matchDetail.HomeId == TeamIdNotAvailable;
        var isAwayIdNotAvailable = matchDetail.AwayId == TeamIdNotAvailable;

        var isScoreInvalid = (isHomeIdNotAvailable && homeScore != 0) || (isAwayIdNotAvailable && awayScore != 0);
        var hasTeamIdNotAvailable = isHomeIdNotAvailable || isAwayIdNotAvailable;

        finalResult.IsValid = !isScoreInvalid && hasTeamIdNotAvailable;
    }

    private static bool IsTeamTotalOrRaidPoints(short childMatchType)
        => childMatchType is TeamTotalPoints or RaidPoints or PlayerRaidPoints;

    private static void ValidateTossWinner(ResultDetail finalResult, int homeScore, int awayScore)
    {
        finalResult.IsValid = (homeScore == 1 && awayScore == 0) || (homeScore == 0 && awayScore == 1);
    }

    private static (int homeScore, int awayScore) ParseScores(ResultDetail finalResult)
    {
        _ = int.TryParse(finalResult.HomeValue, out var homeScore);
        _ = int.TryParse(finalResult.AwayValue, out var awayScore);
        return (homeScore, awayScore);
    }

    private async Task HandleFinalResult(ResultDetail finalResult, Match match)
    {
        var errorMessage = ErrorMessage.InvalidInput.Replace("@score", $"{finalResult.HomeValue}-{finalResult.AwayValue}");

        await resultDetailService.UpdateInvalidResultStatus(match.MatchId, new List<ResultDetail> { finalResult }, errorMessage);

        resultDetailService.WriteLogResult(match.MatchId, finalResult, errorMessage);

        match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != ResourceIds.FullTime).ToList();
    }
}
