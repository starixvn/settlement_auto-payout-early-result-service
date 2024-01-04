namespace AutoPayout.EarlyResult.Services.ExtraStepService;
public interface IExtraStepService
{
    public Task Process(SettleCriteria settleCriteria);
}
