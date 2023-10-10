namespace Fanex.Repository;

public abstract class Condition
{
    protected Condition NextCondition;

    public void SetNextCondition(Condition NextCondition)
    {
        this.NextCondition = NextCondition;
    }

    public abstract Task HandleCondition(int matchId);
}