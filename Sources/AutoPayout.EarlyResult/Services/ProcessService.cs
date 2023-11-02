using AutoPayout.Conditions.Services;
using AutoPayout.Constants;
using AutoPayout.Exceptions;
using AutoPayout.Services;
using AutoPayoutSettlement.Models;
using Microsoft.Extensions.Options;
using Polly;

namespace AutoPayout.EarlyResult.Services;

public interface ProcessService
{
    Task Process(Match match);
}

public class ProcessServiceImp : ProcessService
{
    private readonly WriteLogService writeLogService;

    private readonly CreateConditionChainService createConditionChainService;

    private readonly SettlementService settlementService;

    private readonly MatchPoolService matchPoolService;

    private readonly UpdateResultStatusService updateResultStatusService;

    private readonly AppSettingsKey appSettingsKey;

    public ProcessServiceImp(
        IOptions<AppSettingsKey> options,
        WriteLogService writeLogService,
        CreateConditionChainService createConditionChainService,
        SettlementService settlementService,
        MatchPoolService matchPoolService,
        UpdateResultStatusService updateResultStatusService)
    {
        appSettingsKey = options.Value;
        this.writeLogService = writeLogService;
        this.createConditionChainService = createConditionChainService;
        this.settlementService = settlementService;
        this.matchPoolService = matchPoolService;
        this.updateResultStatusService = updateResultStatusService;
    }

    public async Task Process(Match match)
    {
        try
        {
            var conditions = createConditionChainService.CreateConditionChain(match.Conditions.Split(','));
            await PushAndWriteLog(match, conditions);
        }
        catch (ServiceException exception)
        {
            if (exception.MarkFail)
            {
                await writeLogService.WriteLog(match, exception);
                await updateResultStatusService.UpdateResultStatus(match, MatchStatus.Failed);
            }
        }
        catch (Exception exception)
        {
            writeLogService.ExceptionLog(match.MatchId, exception);
        }
        finally
        {
            matchPoolService.RemoveMatch(match);
        }
    }

    private async Task PushAndWriteLog(Match match, List<ICondition> conditions)
    {
        await settlementService.UpdateResultExpired(match);
        if (!match.ResultDetails.Any())
        {
            return;
        }

        await HandleAndRetryCondition(match, conditions);

        await settlementService.Push(match);
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