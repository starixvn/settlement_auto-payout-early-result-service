namespace Fanex.Repository.Conditions;

public class InitialCondition : Condition
{
    public override Task HandleCondition(int matchId) => NextCondition?.HandleCondition(matchId);
}