using Fanex.AutoPayoutSettlement.Core.Exceptions;
using Fanex.Library;
using Fanex.Library.Constants;

namespace AutoPayoutSettlement.Services;

public interface CreateConditionChainService
{
    List<Condition> CreateConditionChain(string[] conditionIds);
}

public class CreateConditionChainServiceImp : CreateConditionChainService
{
    private readonly Creator creator;

    public CreateConditionChainServiceImp(Creator creator)
    {
        this.creator = creator;
    }

    public List<Condition> CreateConditionChain(string[] conditionIds)
    {
        if (!conditionIds.Any())
        {
            throw new ServiceException(ErrorMessage.MatchNotValid);
        }
        var conditionHandlers = conditionIds.Select(condition => creator.Create(condition)).Where(condition => condition is not null).ToList();

        return conditionHandlers;
    }
}