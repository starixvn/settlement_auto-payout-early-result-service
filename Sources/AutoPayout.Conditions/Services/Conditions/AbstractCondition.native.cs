using AutoPayout.Exceptions;

namespace AutoPayout.Conditions.Services.Conditions;

public abstract class AbstractCondition : ICondition
{
    private ICondition condition;

    public void SetNext(ICondition condition)
    {
        this.condition = condition;
    }

    public abstract Task HandleCondition(Match match);

    protected async Task HandleNextCondition(Match match)
    {
        if (!match.ResultDetails.Any())
        {
            throw new ServiceException { MarkFail = false };
        }

        if (condition != null)
        {
            await condition.HandleCondition(match);
        }
    }
}
