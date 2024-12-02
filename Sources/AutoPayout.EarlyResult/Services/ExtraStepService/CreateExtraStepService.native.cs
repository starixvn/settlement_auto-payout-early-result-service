using AutoPayout.Conditions.Constants;
using AutoPayout.EarlyResult.Services.ExtraStepService.AfterSettle;
using AutoPayout.EarlyResult.Services.ExtraStepService.BeforeSettle;

namespace AutoPayout.EarlyResult.Services.ExtraStepService;
public interface CreateExtraStepService
{
    IExtraStepService Create(Match match);

    IExtraStepService CreateStepAfterSettle(Match match);
}

public class CreateExtraStepServiceImp(
    InitExtraStepService extraStepService,
    BasketBallCashOut basketBallCashOut,
    BasketballExtraStep basketballExtraStep,
    TennisInsResourceResult tennisExtraStep,
    ESportsAddResourceRefund eSportExtraStep) : CreateExtraStepService
{
    public IExtraStepService Create(Match match)
    {
        return match.SportType == SportType.BasketBall ? basketballExtraStep : extraStepService;
    }

    public IExtraStepService CreateStepAfterSettle(Match match)
    {
        return match.SportType switch
        {
            SportType.ESports => eSportExtraStep,
            SportType.BaseBall => eSportExtraStep,
            SportType.VolleyBall => eSportExtraStep,
            SportType.Tennis => tennisExtraStep,
            SportType.BasketBall => basketBallCashOut,
            _ => extraStepService
        };
    }
}

