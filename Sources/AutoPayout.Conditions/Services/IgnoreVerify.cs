namespace AutoPayout.Conditions.Services;

public class IgnoreVerify : ICondition
{
    public Task HandleCondition(Match match)
    {
        return Task.CompletedTask;
    }
}