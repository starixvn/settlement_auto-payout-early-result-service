
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

    public override async Task HandleCondition(Match match)
    {
        var matchDetail = match.MatchDetail;
        var finalResult = match.ResultDetails.Find(i => i.ResourceId == ResourceIds.FullTime);

        if (matchDetail.ChildMatchType == TossWinner)
        {
            // TODO
        }

        int[] naChildMatchType = [TeamTotalPoints, RaidPoints, PlayerRaidPoints];
        if (naChildMatchType.Contains(matchDetail.ChildMatchType))
        {
            // TODO
        }

        if (!finalResult.IsValid)
        {
            await HandleFinalResult(finalResult, match);
        }

        await HandleNextCondition(match);
    }

    private async Task HandleFinalResult(ResultDetail finalResult, Match match)
    {
        var errorMessage = ErrorMessage.InvalidInput.Replace("@score", $"{finalResult.HomeValue}-{finalResult.AwayValue}");

        await resultDetailService.UpdateInvalidResultStatus(match.MatchId, new List<ResultDetail> { finalResult }, errorMessage);

        resultDetailService.WriteLogResult(match.MatchId, finalResult, errorMessage);

        match.ResultDetails = match.ResultDetails.Where(r => r.ResourceId != ResourceIds.FullTime).ToList();
    }
}
