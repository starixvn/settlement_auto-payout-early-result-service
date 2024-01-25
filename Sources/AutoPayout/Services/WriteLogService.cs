using AutoPayout.Exceptions;
using Fanex.Logging;
using Fanex.Logging.Extensions;
using Fanex.Logging.Extensions.Database;
using Match = AutoPayout.Models.Match;

namespace AutoPayout.Services;

public interface WriteLogService
{
    Task ExceptionLog(int matchId, Exception exception);

    Task WriteLog(Match match, ServiceException exception);

    Task WriteLog(Match match, string message);

    Task WriteLogDirectly(int matchId, string message);

    Task InsertResettleLog(SettleCriteria criteria, int sportType, bool isMainMatch);
}

public class WriteLogServiceImp(IDynamicRepository repository) : WriteLogService
{
    public async Task ExceptionLog(int matchId, Exception exception)
    {
        var logMessage = new LogMessage($"Match Id: {matchId} - {exception.Message}")
            .AddSection("Database Info", DatabaseInfoGetters.GetDbExceptionInfo(exception))
            .AddSection("Server Info", CommonInfoGetters.GetServerInfo())
            .AddSection("Exception Info", CommonInfoGetters.GetExceptionInfo(exception))
            .ToString();

        await LogService.ErrorAsync(logMessage);
    }

    public async Task WriteLog(Match match, ServiceException exception)
    {
        var message = exception.Description;
        var errorMessage = GenerateErrorLogMessage(match, message);
        var criteria = new WriteLogCriteria(match.MatchId, errorMessage);
        await repository.ExecuteAsync(criteria);
    }

    public async Task WriteLog(Match match, string message)
    {
        var errorMessage = GenerateErrorLogMessage(match, message);
        var criteria = new WriteLogCriteria(match.MatchId, errorMessage);
        await repository.ExecuteAsync(criteria);
    }

    public async Task WriteLogDirectly(int matchId, string message)
    {
        try
        {
            var criteria = new LogDirectlyCriteria
            {
                Extend = matchId,
                Doing = message,
            };

            await repository.ExecuteAsync(criteria);
        }
        catch (Exception e)
        {
            await LogService.ErrorAsync("AutoPayout.EarlyResult.Service(Write Log Directly): ", e);
        }
    }

    public async Task InsertResettleLog(SettleCriteria criteria, int sportType, bool isMainMatch)
    {
        try
        {
            if (isMainMatch)
            {
                var insertLogCriteria = new InsertLogCriteria(criteria, sportType);
                await repository.ExecuteAsync(insertLogCriteria);
            }
        }
        catch (Exception e)
        {
            await LogService.ErrorAsync("AutoPayout.EarlyResult.Service(Write resettle Log): ", e);
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