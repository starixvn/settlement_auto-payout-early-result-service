using AutoPayout.Conditions.Constants;

namespace AutoPayout.EarlyResult.Services.ExtraStepService;
public interface CreateExtraStepService
{
    IExtraStepService Create(int sportType);
}

public class CreateExtraStepServiceImp(TennisExtraStep tennisExtraStep) : CreateExtraStepService
{
    public IExtraStepService Create(int sportType)
    {
        return sportType switch
        {
            SportType.Tennis => tennisExtraStep,
            _ => throw new NotImplementedException()
        };
    }
}
