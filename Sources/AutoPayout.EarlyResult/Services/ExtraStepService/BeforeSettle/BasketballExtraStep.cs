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
            settleCriteria.DeterminedType = BasketBallHelperService.GenerateDeterminedType(match);
        }

        settleCriteria.BetStatus = match.MatchDetail.GameSession == GameType.Bo4 ?
            BasketBallHelperService.GetBetStatusBoFour(match) : BasketBallHelperService.DefaultBetStatus;

        return Task.CompletedTask;
    }
}
