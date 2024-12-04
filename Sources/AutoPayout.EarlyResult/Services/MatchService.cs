using AutoPayout.Conditions.Constants;
using AutoPayout.EarlyResult.Constants;
using AutoPayout.EarlyResult.Criteria;
using Fanex.Caching;
using Fanex.Data.Repository;
using Microsoft.Extensions.Options;

namespace AutoPayout.EarlyResult.Services;

public interface MatchService
{
    Task<List<Match>> GetMatches();
}

public class MatchServiceImp(IOptions<AppSettingsKey> appSettingsKey, ICache cache, IDynamicRepository repository)
    : MatchService
{
    public async Task<List<Match>> GetMatches()
    {
        var matches = await FetchMatch();

        return await MatchBranching(matches);
    }

    private async Task<List<Match>> MatchBranching(List<Match> matches)
    {
        if (appSettingsKey.Value.GroupTest > 0)
        {
            return matches;
        }

        var (_, response) = await cache.TryGetAsync<string>(appSettingsKey.Value.CacheKey);
        var leagueIds = (response ?? "").Split(",");

        return appSettingsKey.Value.EnvironmentName switch
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
            AutoPayoutGroup = appSettingsKey.Value.GroupTest > 0 ? appSettingsKey.Value.GroupTest : appSettingsKey.Value.AutoPayoutGroup,
        };

        var (firstResultSet, secondResultSet) = await repository.FetchMultipleAsync<ConditionSetting, ResultDetail>(criteria);

        return IgnoreResult(secondResultSet).GroupBy(r => new { r.MatchId, r.LeagueId, r.SportType, r.ResourceType }).Select(g => new Match
        {
            MatchId = g.Key.MatchId,
            LeagueId = g.Key.LeagueId,
            ResultDetails = g.ToList(),
            ResourceType = g.Key.ResourceType,
            SportType = g.Key.SportType,
            Conditions = firstResultSet.Where(r => r.SportType == g.Key.SportType)
               .Select(r => g.Key.ResourceType == ResourceType.MainResource ? r.MainCheckList : r.EarlyCheckList)
               .FirstOrDefault(string.Empty),
            EarlyRetryTime = g.First().EarlyRetryTime,
            MainRetryTime = g.First().MainRetryTime,
        }).Where(m => !string.IsNullOrEmpty(m.Conditions)).ToList();
    }

    private static IEnumerable<ResultDetail> IgnoreResult(IEnumerable<ResultDetail> resultDetails)
    {
        return resultDetails.Where(r => !IgnoreResultByCondition.Conditions.TryGetValue(r.SportType, out var condition) || condition(r));
    }
}