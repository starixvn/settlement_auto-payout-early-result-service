namespace AutoPayout.Conditions.Services;

public interface ICondition
{
    public Task HandleCondition(Match match);
}