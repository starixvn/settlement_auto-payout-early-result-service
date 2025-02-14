using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Exceptions;
using AutoPayout.Services;
using Microsoft.Extensions.Options;

namespace AutoPayout.EarlyResult.Services;

public interface ProcessService
{
    Task Process(Match match);
}

public class ProcessServiceImp(IOptions<AppSettingsKey> appSettingsKey,
        WriteLogService writeLogService,
        CreateConditionChainService createConditionChainService,
        SettlementService settlementService,
        MatchPoolService matchPoolService,
        MatchService matchService,
        UpdateResultStatusService updateResultStatusService)
    : ProcessService
{
    public async Task Process(Match match)
    {
        try
        {
            var conditions = createConditionChainService.CreateConditionChain(match.Conditions.Split(','));

            match.ResultDetails = Filter(match);

            await PushAndWriteLog(match, conditions);
        }
        catch (ServiceException exception)
        {
            await ProcessError(match, exception);
        }
        catch (Exception exception)
        {
            await writeLogService.ExceptionLog(match, exception);
        }
        finally
        {
            matchPoolService.RemoveMatch(match);
        }
    }

    private async Task PushAndWriteLog(Match match, List<ICondition> conditions)
    {
        await settlementService.UpdateResultExpired(match);
        if (match.ResultDetails.Count == 0)
        {
            return;
        }

        if (appSettingsKey.Value.GroupTest != 0)
        {
            await HandleAndRetryCondition(match, conditions);
            await writeLogService.WriteLog(match, string.Empty);
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
            await updateResultStatusService.UpdateResultStatus(match, MatchStatus.Failed, exception.Description);
        }
    }

    private async Task HandleAndRetryCondition(Match match, List<ICondition> conditions)
    {
        for (var i = 0; i < conditions.Count - 1; i++)
        {
            conditions[i].SetNext(conditions[i + 1]);
        }

        await conditions[0].HandleCondition(match);
    }

    private List<ResultDetail> Filter(Match match)
    {
        if (match.MatchDetail.ParentID != 0)
        {
            return match.ResultDetails.Where(x => x.ResourceId == ResourceIds.FullTime).ToList();
        }

        return match.ResultDetails;
    }
}