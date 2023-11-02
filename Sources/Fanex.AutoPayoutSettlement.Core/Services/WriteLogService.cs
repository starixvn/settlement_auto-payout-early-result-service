using Fanex.AutoPayoutSettlement.Core.Criteria;
using Fanex.AutoPayoutSettlement.Core.Exceptions;
using Fanex.Data.Repository;
using Fanex.Logging;
using Fanex.Logging.Extensions;
using Fanex.Logging.Extensions.Database;
using Match = Fanex.AutoPayoutSettlement.Core.Models.Match;

namespace Fanex.AutoPayoutSettlement.Core.Services;

public interface WriteLogService
{
    void ExceptionLog(int matchId, Exception exception);

    Task WriteLog(Match matchId, ServiceException exception);

    void WriteLog(Match match, string message);

    void WriteLogDirectly(int matchId, string message);

    void InsertResettleLog(SettleCriteria criteria, int sportType, bool isMainMatch);
}

public class WriteLogServiceImp : WriteLogService
{
    private readonly IDynamicRepository repository;

    public WriteLogServiceImp(IDynamicRepository repository)
    {
        this.repository = repository;
    }

    public void ExceptionLog(int matchId, Exception exception)
    {
        var logMessage = new LogMessage($"Match Id: {matchId} - {exception.Message}")
            .AddSection("Database Info", DatabaseInfoGetters.GetDbExceptionInfo(exception))
            .AddSection("Server Info", CommonInfoGetters.GetServerInfo())
            .AddSection("Exception Info", CommonInfoGetters.GetExceptionInfo(exception))
            .ToString();
        LogService.Error(logMessage);
    }

    public async Task WriteLog(Match match, ServiceException exception)
    {
        var message = exception.Description;
        var errorMessage = GenerateErrorLogMessage(match, message);
        var criteria = new WriteLogCriteria(match.MatchId, errorMessage);
        await repository.ExecuteAsync(criteria);
    }

    public void WriteLog(Match match, string message)
    {
        var errorMessage = GenerateErrorLogMessage(match, message);
        var criteria = new WriteLogCriteria(match.MatchId, errorMessage);
        repository.Execute(criteria);
    }

    public void WriteLogDirectly(int matchId, string message)
    {
        try
        {
            var criteria = new LogDirectlyCriteria
            {
                Extend = matchId,
                Doing = message,
            };

            repository.Execute(criteria);
        }
        catch (Exception e)
        {
            LogService.Error("Fanex.AutoPayoutSettlement.Service(Write Log Directly): ", e);
        }
    }

    public void InsertResettleLog(SettleCriteria criteria, int sportType, bool isMainMatch)
    {
        try
        {
            if (isMainMatch)
            {
                var insertLogCriteria = new InsertLogCriteria(criteria, sportType);
                repository.Execute(insertLogCriteria);
            }
        }
        catch (Exception e)
        {
            LogService.Error("Fanex.AutoPayoutSettlement.Service(Write resettle Log): ", e);
        }
    }

    private static string GenerateErrorLogMessage(Match match, string message)
    {
        var resourceIds = match.ResultDetails.Select(resultDetail => resultDetail.ResourceId).ToList();

        var resources = string.Join(",", resourceIds);
        var title = $"NAP: Auto Payout: {resources}";
        var remark = new List<string>
        {
            title, message
        };

        return string.Join("<br/>", remark);
    }
}