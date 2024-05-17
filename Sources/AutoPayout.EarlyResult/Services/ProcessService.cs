using AutoPayout.Conditions.Services;
using AutoPayout.Exceptions;
using AutoPayout.Services;
using Microsoft.Extensions.Options;
using Polly;

namespace AutoPayout.EarlyResult.Services;

public interface ProcessService
{
    Task Process(Match match);
}

public class ProcessServiceImp(IOptions<AppSettingsKey> options,
        WriteLogService writeLogService,
        CreateConditionChainService createConditionChainService,
        SettlementService settlementService,
        MatchPoolService matchPoolService,
        UpdateResultStatusService updateResultStatusService)
    : ProcessService
{
    private readonly AppSettingsKey appSettingsKey = options.Value; //NOSONAR


    public async Task Process(Match match)
    {
        try
        {
            var conditions = createConditionChainService.CreateConditionChain(match.Conditions.Split(','));
            await PushAndWriteLog(match, conditions);
        }
        catch (ServiceException exception)
        {
            await ProcessError(match, exception);
        }
        catch (Exception exception)
        {
            await writeLogService.ExceptionLog(match.MatchId, exception);
        }
        finally
        {
            matchPoolService.RemoveMatch(match);
        }
    }

    private async Task PushAndWriteLog(Match match, List<ICondition> conditions)
    {
        if (appSettingsKey.GroupTest != 0)
        {
            await HandleAndRetryCondition(match, conditions);
            await writeLogService.WriteLog(match, string.Empty);
            return;
        }

        await settlementService.UpdateResultExpired(match);
        if (!match.ResultDetails.Any())
        {
            return;
        }

        await HandleAndRetryCondition(match, conditions);
        await settlementService.Push(match);
    }

    private async Task ProcessError(Match match, ServiceException exception)
    {
        if (exception.MarkFail)
        {
            await writeLogService.WriteLog(match, exception);
            if (appSettingsKey.GroupTest == 0)
            {
                await updateResultStatusService.UpdateResultStatus(match, MatchStatus.Failed, exception.Description);
            }
        }
    }

    private async Task HandleAndRetryCondition(Match match, List<ICondition> conditions)
    {
        await Policy.Handle<ServiceException>(e => e.NeedRetry)
            .WaitAndRetryAsync(appSettingsKey.RetryCount,
                _ => TimeSpan.FromMilliseconds(appSettingsKey.RetryInterval))
            .ExecuteAsync(async () =>
            {
                foreach (var condition in conditions)
                {
                    await condition.HandleCondition(match);
                }
            });
    }
}