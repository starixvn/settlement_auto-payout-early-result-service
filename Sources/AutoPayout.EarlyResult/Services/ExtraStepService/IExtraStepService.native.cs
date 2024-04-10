namespace AutoPayout.EarlyResult.Services.ExtraStepService;
public interface IExtraStepService
{
    public Task Process(Match match);
}


public class InitExtraStepService : IExtraStepService
{
    public Task Process(Match match)
    {
        return Task.CompletedTask;
    }
}