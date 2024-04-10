using AutoPayout.Conditions.Constants;

namespace AutoPayout.EarlyResult.Services.ExtraStepService;
public interface CreateExtraStepService
{
    IExtraStepService Create(Match match);
}

public class CreateExtraStepServiceImp(TennisExtraStep tennisExtraStep, InitExtraStepService extraStepService) : CreateExtraStepService
{
    public IExtraStepService Create(Match match)
    {
        return match.SportType == SportType.Tennis ? tennisExtraStep : extraStepService;
    }
}
