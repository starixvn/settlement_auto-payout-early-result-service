using AutoPayout.Conditions.Constants;
using AutoPayout.EarlyResult.Services.ExtraStepService.HelperServices;

namespace AutoPayout.EarlyResult.Services.ExtraStepService.BeforeSettle;
public class BasketballExtraStep : IExtraStepService
{
    public Task Process(Match match, SettleCriteria settleCriteria)
    {
        if (match.ResourceType != ResourceType.MainResource)
        {
            return Task.CompletedTask;
        }

        const int instantBit = 1 << 1;

        if ((match.MatchDetail.AutoSetting & instantBit) != 0)
        {
            settleCriteria.DeterminedType = GenerateDeterminedType(match);
        }

        settleCriteria.BetStatus = match.MatchDetail.GameSession == GameType.Bo4 ?
            BasketBallBetStatusService.GetBetStatusBoFour(match) : BasketBallBetStatusService.DefaultBetStatus;

        return Task.CompletedTask;
    }

    private static byte GenerateDeterminedType(Match match)
    {
        const int roundBitStartAt = 3;

        var determinedType = match.ResultDetails.Select(resultDetail => resultDetail.ResourceId switch
            {
                ResourceIds.FullTime => 0,
                ResourceIds.Halftime => 2,
                _ => Array.FindIndex(ResourceIds.ResultBasketBall, r => r == resultDetail.ResourceId) + roundBitStartAt
            })
            .Select(bitPosition => Math.Pow(2, bitPosition))
            .Sum();

        return (byte)determinedType;
    }
}
