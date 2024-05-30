using AutoPayout.Conditions.Constants;
using AutoPayout.EarlyResult.Constants;
using AutoPayout.EarlyResult.Criteria;
using AutoPayout.EarlyResult.Models;
using AutoPayout.EarlyResult.Services.ExtraStepService;
using AutoPayout.Services;
using Fanex.Data.Repository;
using Fanex.Settlement.ScaleOut.Request;
using Fanex.Settlement.ScaleOut.Services;
using Microsoft.Extensions.Options;

namespace AutoPayout.EarlyResult.Services;

public interface SettlementService
{
    Task Push(Match match);

    Task UpdateResultExpired(Match match);
}

public class SettlementServiceImp(IOptions<AppSettingsKey> options,
        WriteLogService writeLogService,
        IDynamicRepository repository,
        ScaleOutSettleService scaleOutSettleService,
        CreateQueryService createQueryService,
        UpdateResultStatusService updateResultStatusService,
        CreateExtraStepService createExtraStepService)
    : SettlementService
{
    private readonly AppSettingsKey appSettingsKey = options.Value; //NOSONAR

    public async Task Push(Match match)
    {
        if (match.ResultDetails.Any())
        {
            var settleCriteria = createQueryService.QuerySettle(match);
            var isExistedFinalResource = IsExistedFinalResource(match.ResultDetails);
            await Execute(settleCriteria, match, isExistedFinalResource);
            await SettleHedgeMatch(settleCriteria, match, isExistedFinalResource);
            await AddResourceRefund(match);
        }
    }

    public async Task UpdateResultExpired(Match match)
    {
        var resultExpired = match.ResultDetails.Where(r => r.ResourceStatus == ResourceStatus.Expired).ToList();
        if (resultExpired.Any())
        {
            match.ResultDetails = match.ResultDetails.Except(resultExpired);
            var resultExpiredIds = string.Join(',', resultExpired.Select(r => r.ResourceId));
            var cloneMatch = new Match
            {
                MatchId = match.MatchId,
                ResultDetails = resultExpired
            };

            var remark = string.Format(ErrorMessage.ResultExpired, resultExpiredIds);
            await writeLogService.WriteLog(cloneMatch, remark);
            await updateResultStatusService.UpdateResultStatus(cloneMatch, MatchStatus.Failed, remark);
        }
    }

    private async Task SettleHedgeMatch(SettleCriteria criteria, Match match, bool isExistedFinalResource)
    {
        var hedgeMatchDetail = await GetHedgeMatch(criteria.MatchId);
        if (hedgeMatchDetail != null)
        {
            criteria.MatchId = hedgeMatchDetail.MatchId;
            await Execute(criteria, match, isExistedFinalResource, false);
        }
    }

    private async Task Execute(SettleCriteria criteria, Match match, bool isExistedFinalResource, bool isMainMatch = true)
    {
        await SetUnderSettleFlag(criteria, isExistedFinalResource);

        await UpdateMatchInfo(criteria);

        if (isMainMatch)
        {
            var extraStep = createExtraStepService.Create(match);
            await extraStep.Process(match);
        }

        await writeLogService.WriteLogDirectly(criteria.MatchId, GenerateLogMessage(criteria));
        await writeLogService.InsertResettleLog(criteria, match.SportType, isMainMatch);

        foreach (var server in appSettingsKey.SiteDbServer)
        {
            criteria.DbServer = server;
            await repository.ExecuteAsync(criteria);
        }
    }

    private async Task AddResourceRefund(Match match)
    {
        if (match.SportType == SportType.ESports && !string.IsNullOrEmpty(match.ResourceRefund))
        {
            var criteria = new AddResourceRefundCriteria
            {
                MatchId = match.MatchId,
                ResourceIds = match.ResourceRefund
            };
            await repository.ExecuteAsync(criteria);
        }
    }

    private async Task<HedgeMatchDetail> GetHedgeMatch(int matchId)
    {
        var criteria = new GetHedgeMatchCriteria { MatchId = matchId };
        return await repository.GetAsync<HedgeMatchDetail>(criteria);
    }

    private static bool IsExistedFinalResource(IEnumerable<ResultDetail> resultDetails)
    {
        return resultDetails.Any(resource => !string.IsNullOrEmpty(resource.ResourceId) && resource.ResourceId.Equals(ResourceId.Final));
    }

    private async Task SetUnderSettleFlag(SettleCriteria criteria, bool isExistedFinalResource) //NOSONAR
    {
        if (isExistedFinalResource)
        {
            await scaleOutSettleService.UpdateUnderSettlementFlag(criteria.MatchId, criteria.AdminId, criteria.Action);
        }
    }

    private static string GenerateLogMessage(SettleCriteria criteria)
        => $"NAP: Auto Payout Calculate {criteria.Action} for matchId: {criteria.MatchId} Result: {criteria.SelectedResources}";

    private async Task UpdateMatchInfo(SettleCriteria criteria)
    {
        var requestUpdateInfo = new UpdateMatchInfoRequest
        {
            Action = criteria.Action,
            AdminId = criteria.AdminId,
            MatchId = criteria.MatchId,
            SelectedResources = criteria.SelectedResources,
            IsAuto = true
        };

        await scaleOutSettleService.UpdateMatchInfo(requestUpdateInfo);
    }
}