using AutoPayoutSettlement.Constants;
using AutoPayoutSettlement.Criteria;
using AutoPayoutSettlement.Models;
using Fanex.Caching;
using Fanex.Data.Repository;
using Microsoft.Extensions.Options;
using System.Collections.Immutable;

namespace AutoPayout.EarlyResult.Services;

public interface MatchService
{
    Task<List<Match>> GetMatches();
}

public class MatchServiceImp : MatchService
{
    private readonly AppSettingsKey appSettingsKey;
    private readonly ICache cache;
    private readonly IDynamicRepository repository;

    public MatchServiceImp(IOptions<AppSettingsKey> options, ICache cache, IDynamicRepository repository)
    {
        appSettingsKey = options.Value;
        this.cache = cache;
        this.repository = repository;
    }

    public async Task<List<Match>> GetMatches()
    {
        var matches = await FetchMatch();

        var (_, response) = await cache.TryGetAsync<string>(appSettingsKey.CacheKey);
        var leagueIds = (response ?? "").Split(",");

        return appSettingsKey.EnvironmentName switch
        {
            "PRO" => leagueIds.Length > 0
                ? matches.Where(m => !leagueIds.Contains(m.LeagueId.ToString())).ToList()
                : matches,
            "UAT" => matches.Where(m => leagueIds.Contains(m.LeagueId.ToString())).ToList(),
            _ => matches
        };
    }

    private async Task<List<Match>> FetchMatch()
    {
        var criteria = new GetResultCriteria
        {
            AutoPayoutGroup = appSettingsKey.AutoPayoutGroup,
            ResourceType = appSettingsKey.ResourceType
        };

        var results = await repository.FetchMultipleAsync<ConditionSetting, ResultDetail>(criteria);

        return results.SecondResultSet.GroupBy(r => new { r.MatchId, r.LeagueId, r.SportType, r.ResourceType }).Select(g => new Match
        {
            MatchId = g.Key.MatchId,
            LeagueId = g.Key.LeagueId,
            ResultDetails = g.ToImmutableList(),
            ResourceType = g.Key.ResourceType,
            SportType = g.Key.SportType,
            Conditions = results.FirstResultSet.Where(r => r.SportType == g.Key.SportType)
                .Select(r => g.Key.ResourceType == ResourceType.MainResource ? r.MainCheckList : r.EarlyCheckList)
                .FirstOrDefault(string.Empty)
        }).Where(m => !string.IsNullOrEmpty(m.Conditions)).ToList();
    }
}