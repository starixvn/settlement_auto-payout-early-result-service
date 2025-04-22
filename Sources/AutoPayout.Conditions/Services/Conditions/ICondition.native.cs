namespace AutoPayout.Conditions.Services.Conditions;

public interface ICondition
{
    public Task HandleCondition(Match match);

    public void SetNext(ICondition condition);
}