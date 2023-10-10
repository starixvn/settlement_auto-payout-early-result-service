using Fanex.Repository.Conditions;
using Fanex.Data.Repository;

namespace Fanex.Repository;

public interface ICreator
{
    Condition Create(string id);
}

public class Creator : ICreator
{
    private readonly IDynamicRepository repository;

    public Creator(IDynamicRepository repository)
    {
        this.repository = repository;
    }

    public Condition Create(string id)
    {
        return id switch
        {
            "1" => new VerifyHoldMatch(repository),
            "2" => new VerifyMatchStatus(),
            "3" => new VerifyRunningOdds(),
            _ => new InitialCondition()
        };
    }
}