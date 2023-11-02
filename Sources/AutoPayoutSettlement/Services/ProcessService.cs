using AutoPayoutSettlement.Models;
using Fanex.AutoPayoutSettlement.Core.Constants;
using Fanex.AutoPayoutSettlement.Core.Exceptions;
using Fanex.AutoPayoutSettlement.Core.Models;
using Fanex.AutoPayoutSettlement.Core.Services;
using Fanex.Library;
using Microsoft.Extensions.Options;
using Polly;

namespace AutoPayoutSettlement.Services;

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

    private async Task PushAndWriteLog(Match match, List<Condition> conditions)
    {
        await settlementService.UpdateResultExpired(match);
        if (!match.ResultDetails.Any())
        {
            return;
        }

        await HandleAndRetryCondition(match, conditions);

        await settlementService.Push(match);
    }

    private async Task HandleAndRetryCondition(Match match, List<Condition> conditions)
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