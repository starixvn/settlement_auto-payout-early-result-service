using AutoPayout.Conditions.Constants;
using AutoPayout.EarlyResult.Services.ExtraStepService.AfterSettle;
using AutoPayout.EarlyResult.Services.ExtraStepService.BeforeSettle;

namespace AutoPayout.EarlyResult.Services.ExtraStepService;
public interface CreateExtraStepService
{
    IExtraStepService Create(Match match);

    IEnumerable<IExtraStepService> CreateStepsServiceAfterSettle(Match match);
}

public class CreateExtraStepServiceImp(
    InitExtraStepService extraStepService,
    BasketBallCashOut basketBallCashOut,
    BasketballExtraStep basketballExtraStep,
    TennisInsResourceResult tennisExtraStep,
    AddResourceRefund addResourceRefund) : CreateExtraStepService
{
    public IExtraStepService Create(Match match)
    {
        return match.SportType == SportType.BasketBall ? basketballExtraStep : extraStepService;
    }

    public IEnumerable<IExtraStepService> CreateStepsServiceAfterSettle(Match match)
    {
        return match.SportType switch
        {
            SportType.ESports or SportType.BaseBall or SportType.VolleyBall or SportType.TableTennis or SportType.Badminton or SportType.BeachVolleyball => [addResourceRefund],
            SportType.Tennis when match.MatchDetail.ParentID == 0 => [addResourceRefund, tennisExtraStep],
            SportType.BasketBall => [basketBallCashOut],
            _ => [extraStepService]
        };
    }
}

