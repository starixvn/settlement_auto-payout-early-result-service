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
    private readonly AppSettingsKey appSettingsKey = options.Value;

    public async Task Push(Match match)
    {
        var settleCriteria = createQueryService.QuerySettle(match);
        var isExistedFinalResource = IsExistedFinalResource(match.ResultDetails);
        await Execute(settleCriteria, match.SportType, isExistedFinalResource);
        await SettleHedgeMatch(settleCriteria, match.SportType, isExistedFinalResource);
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

            await writeLogService.WriteLog(cloneMatch, string.Format(ErrorMessage.ResultExpired, resultExpiredIds));
            await updateResultStatusService.UpdateResultStatus(cloneMatch, MatchStatus.Failed);
        }
    }

    private async Task SettleHedgeMatch(SettleCriteria criteria, int sportType, bool isExistedFinalResource)
    {
        var hedgeMatchDetail = await GetHedgeMatch(criteria.MatchId);
        if (hedgeMatchDetail != null)
        {
            criteria.MatchId = hedgeMatchDetail.MatchId;
            await Execute(criteria, sportType, isExistedFinalResource, false);
        }
    }

    private async Task Execute(SettleCriteria criteria, int sportType, bool isExistedFinalResource, bool isMainMatch = true)
    {
        await SetUnderSettleFlag(criteria, isExistedFinalResource);

        await UpdateMatchInfo(criteria);

        var extraStep = createExtraStepService.Create(sportType);

        await extraStep.Process(criteria);
        await writeLogService.WriteLogDirectly(criteria.MatchId, GenerateLogMessage(criteria));
        await writeLogService.InsertResettleLog(criteria, sportType, isMainMatch);

        foreach (var server in appSettingsKey.SiteDbServer)
        {
            criteria.DbServer = server;
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

    private async Task SetUnderSettleFlag(SettleCriteria criteria, bool isExistedFinalResource)
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