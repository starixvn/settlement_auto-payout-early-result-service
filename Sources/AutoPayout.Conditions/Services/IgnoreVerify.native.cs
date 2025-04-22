using AutoPayout.Conditions.Services.Conditions;

namespace AutoPayout.Conditions.Services;

public class IgnoreVerify : AbstractCondition
{
    public override Task HandleCondition(Match match)
    {
        return Task.CompletedTask;
    }
}