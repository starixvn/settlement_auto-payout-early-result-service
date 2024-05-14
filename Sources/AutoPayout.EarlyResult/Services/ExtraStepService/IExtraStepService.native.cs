namespace AutoPayout.EarlyResult.Services.ExtraStepService;
public interface IExtraStepService
{
    public Task Process(Match match, SettleCriteria settleCriteria);
}


public class InitExtraStepService : IExtraStepService
{
    public Task Process(Match match, SettleCriteria settleCriteria)
    {
        return Task.CompletedTask;
    }
}