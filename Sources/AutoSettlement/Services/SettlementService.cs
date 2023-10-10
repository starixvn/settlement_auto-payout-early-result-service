using Fanex.Logging;
using Fanex.Logging.Extensions;
using Fanex.Logging.Extensions.Database;
using Fanex.Repository;
using Fanex.Repository.Conditions;
using Fanex.Repository.Exceptions;
using Fanex.Repository.Models;

namespace AutoSettlement.Services;

public interface ISettlementService
{
    void Process(Match match);
}

public class SettlementService : ISettlementService
{
    private readonly ICreator _creator;

    public SettlementService(ICreator creator)
    {
        _creator = creator;
    }

    public void Process(Match match)
    {
        try
        {
            var chainCondition = BuildChainCondition(match.Conditions);
            chainCondition.HandleCondition(match.MatchId);
        }
        catch (ServiceException)
        {
        }
        catch (Exception exception)
        {
            var logMessage = new LogMessage($"Match Id: {match.MatchId} {exception.Message}")
                .AddSection("Database Info", DatabaseInfoGetters.GetDbExceptionInfo(exception))
                .AddSection("Server Info", CommonInfoGetters.GetServerInfo())
                .AddSection("Exception Info", CommonInfoGetters.GetExceptionInfo(exception))
                .ToString();
            LogService.Error(logMessage);
        }
    }

    private Condition BuildChainCondition(string conditionIds)
    {
        var conditions = conditionIds.Split(',');
        var conditionHandlers = conditions.Select(condition => _creator.Create(condition)).ToList();

        for (var i = 1; i < conditionHandlers.Count; i++)
        {
            conditionHandlers[i - 1].SetNextCondition(conditionHandlers[i]);
        }

        return conditionHandlers.FirstOrDefault(new InitialCondition());
    }
}