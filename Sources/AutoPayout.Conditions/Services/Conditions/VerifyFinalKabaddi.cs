
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
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
        if (matchDetail.ChildMatchType == TossWinner)
        {
            // TODO
        }

        int[] naChildMatchType = [TeamTotalPoints, RaidPoints, PlayerRaidPoints];
        if (naChildMatchType.Contains(matchDetail.ChildMatchType))
        {
            // TODO
        }

        await HandleNextCondition(match);
    }
}
