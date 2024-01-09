using AutoPayout.Conditions;
using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Services;
using AutoPayout.Exceptions;

namespace AutoPayout.EarlyResult.Services;

public interface CreateConditionChainService
{
    List<ICondition> CreateConditionChain(string[] conditionIds);
}

public class CreateConditionChainServiceImp : CreateConditionChainService
{
    private readonly Creator creator;

    public CreateConditionChainServiceImp(Creator creator)
    {
        this.creator = creator;
    }

    public List<ICondition> CreateConditionChain(string[] conditionIds)
    {
        if (!conditionIds.Any())
        {
            throw new ServiceException(ErrorMessage.MatchNotValid);
        }
        var conditionHandlers = conditionIds.Select(condition => creator.Create(condition)).Where(condition => condition is not null).ToList();

        return conditionHandlers;
    }
}