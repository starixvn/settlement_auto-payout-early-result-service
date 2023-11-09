using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Services;
using AutoPayout.EarlyResult.Constants;
using AutoPayout.EarlyResult.Criteria;
using AutoPayout.EarlyResult.Models;
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

public class SettlementServiceImp : SettlementService
{
    private readonly WriteLogService writeLogService;

    private readonly IDynamicRepository repository;

    private readonly CreateQueryService createQueryService;

    private readonly AppSettingsKey appSettingsKey;

    private readonly ScaleOutSettleService scaleOutSettleService;

    private readonly UpdateResultStatusService updateResultStatusService;

    private readonly ScoreService scoreService;

    public SettlementServiceImp(IOptions<AppSettingsKey> options,
        WriteLogService writeLogService,
        IDynamicRepository repository,
        ScaleOutSettleService scaleOutSettleService,
        CreateQueryService createQueryService,
        UpdateResultStatusService updateResultStatusService,
        ScoreService scoreService)
    {
        appSettingsKey = options.Value;
        this.writeLogService = writeLogService;
        this.repository = repository;
        this.scaleOutSettleService = scaleOutSettleService;
        this.createQueryService = createQueryService;
        this.updateResultStatusService = updateResultStatusService;
        this.scoreService = scoreService;
    }

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
            await LogResultExpired(match, resultExpired);
        }
    }

    private async Task LogResultExpired(Match match, IReadOnlyCollection<ResultDetail> resultExpired)
    {
        var cloneMatch = new Match
        {
            MatchId = match.MatchId,
            SportType = match.SportType,
            ResultDetails = resultExpired
        };
        var scores = await scoreService.GetScore(cloneMatch);
        var traderScoresSettled = scores
            .Where(s => s.SourceId == 0 && s.SettlementStatusId != SettlementStatus.Running)
            .Select(s => s.ResourceId)
            .ToHashSet();

        var resultRunning = resultExpired
            .Where(r => !traderScoresSettled.Contains(r.ResourceId))
            .ToList();

        var resultSettled = resultExpired.Except(resultRunning).ToList();

        if (resultRunning.Any())
        {
            cloneMatch.ResultDetails = resultRunning;
            await HandleResultRunning(cloneMatch);
        }

        if (resultSettled.Any())
        {
            cloneMatch.ResultDetails = resultSettled;
            await updateResultStatusService.UpdateResultStatus(cloneMatch, MatchStatus.ResultSettled);
        }
    }

    private async Task HandleResultRunning(Match match)
    {
        var resultExpiredIds = string.Join(',', match.ResultDetails.Select(r => r.ResourceId));
        await writeLogService.WriteLog(match, string.Format(ErrorMessage.ResultExpired, resultExpiredIds));
        await updateResultStatusService.UpdateResultStatus(match, MatchStatus.Failed);
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