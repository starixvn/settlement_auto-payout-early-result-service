using AutoPayout.Conditions.Constants;
using AutoPayout.EarlyResult.Constants;
using AutoPayout.EarlyResult.Criteria;
using Fanex.Caching;
using Fanex.Data.Repository;
using Microsoft.Extensions.Options;
using System.Collections.Immutable;

namespace AutoPayout.EarlyResult.Services;

public interface MatchService
{
    Task<List<Match>> GetMatches();
}

public class MatchServiceImp(IOptions<AppSettingsKey> options, ICache cache, IDynamicRepository repository)
    : MatchService
{
    private readonly AppSettingsKey appSettingsKey = options.Value; //NOSONAR

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
            AutoPayoutGroup = appSettingsKey.GroupTest > 0 ? appSettingsKey.GroupTest : appSettingsKey.AutoPayoutGroup,
            ResourceType = appSettingsKey.ResourceType
        };

        var results = await repository.FetchMultipleAsync<ConditionSetting, ResultDetail>(criteria);

        return IgnoreResult(results.SecondResultSet).GroupBy(r => new { r.MatchId, r.LeagueId, r.SportType, r.ResourceType }).Select(g => new Match
        {
            MatchId = g.Key.MatchId,
            LeagueId = g.Key.LeagueId,
            ResultDetails = g.ToList(),
            ResourceType = g.Key.ResourceType,
            SportType = g.Key.SportType,
            Conditions = results.FirstResultSet.Where(r => r.SportType == g.Key.SportType)
               .Select(r => g.Key.ResourceType == ResourceType.MainResource ? r.MainCheckList : r.EarlyCheckList)
               .FirstOrDefault(string.Empty),
            EarlyRetryTime = g.First().EarlyRetryTime,
            MainRetryTime = g.First().MainRetryTime,
        }).Where(m => !string.IsNullOrEmpty(m.Conditions)).ToList();
    }

    private static IEnumerable<ResultDetail> IgnoreResult(IEnumerable<ResultDetail> resultDetails)
    {
        return resultDetails.Where(r => !IgnoreResultByCondition.Conditions.TryGetValue(r.SportType, out var condition) || condition(r.ResourceId));
    }
}