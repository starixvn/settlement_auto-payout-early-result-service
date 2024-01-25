using AutoPayout.Conditions.Constants;

namespace AutoPayout.EarlyResult.Services.ExtraStepService;
public interface CreateExtraStepService
{
    IExtraStepService Create(int sportType);
}

public class CreateExtraStepServiceImp(TennisExtraStep tennisExtraStep, InitExtraStepService extraStepService) : CreateExtraStepService
{
    public IExtraStepService Create(int sportType)
    {
        return sportType == SportType.Tennis ? tennisExtraStep : extraStepService;
    }
}
