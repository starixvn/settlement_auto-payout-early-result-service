namespace AutoPayout.EarlyResult.Services.ExtraStepService;
public interface IExtraStepService
{
    public Task Process(SettleCriteria settleCriteria);
}


public class InitExtraStepService : IExtraStepService
{
    public Task Process(SettleCriteria settleCriteria)
    {
        return Task.CompletedTask;
    }
}