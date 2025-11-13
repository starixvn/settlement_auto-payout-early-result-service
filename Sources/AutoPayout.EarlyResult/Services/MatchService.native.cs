using AutoPayout.Conditions.Constants;
using AutoPayout.Conditions.Criteria;
using AutoPayout.EarlyResult.Constants;
using AutoPayout.EarlyResult.Criteria;
using Fanex.Caching;
using Fanex.Data.Repository;
using Fanex.Logging;
using Fanex.Settlement.Converter.Services;
using Microsoft.Extensions.Options;

namespace AutoPayout.EarlyResult.Services;

public interface MatchService
{
    Task<List<Match>> GetMatches();

    Task<List<Match>> GetMatchDetails(List<Match> matches);
}

public class MatchServiceImp(
    IOptions<AppSettingsKey> appSettingsKey,
    ICache cache,
    IDynamicRepository repository,
    XmlConvertService xmlConvertService
    )
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

        var (_, response) = await cache.TryGetAsync<string>(CacheKey.LeagueId);
        LogService.Info($"MatchBranching: {response}");
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
            AutoPayoutGroup = appSettingsKey.Value.GroupTest > 0
                ? appSettingsKey.Value.GroupTest
                : appSettingsKey.Value.AutoPayoutGroup,
        };

        var (firstResultSet, secondResultSet, thirdResults) =
            await repository.FetchMultipleAsync<ConditionSetting, ResultDetail, ResultDetail>(criteria);

        LogService.Info($"FetchMatch: {string.Join(",", secondResultSet.Select(x => x.MatchId))}");
        await InsertProviderExternal(thirdResults);

        return IgnoreResult(secondResultSet)
            .GroupBy(r => new { r.MatchId, r.LeagueId, r.SportType, r.ResourceType }).Select(g => new Match
            {
                MatchId = g.Key.MatchId,
                LeagueId = g.Key.LeagueId,
                ResultDetails = g.ToList(),
                ResourceType = g.Key.ResourceType,
                SportType = g.Key.SportType,
                Conditions = firstResultSet.Where(r => r.SportType == g.Key.SportType)
                    .Select(r =>
                        g.Key.ResourceType == ResourceType.MainResource ? r.MainCheckList : r.EarlyCheckList)
                    .FirstOrDefault(string.Empty),
                EarlyRetryTime = g.First().EarlyRetryTime,
                MainRetryTime = g.First().MainRetryTime,
            }).Where(m => !string.IsNullOrEmpty(m.Conditions)).ToList();
    }

    private async Task InsertProviderExternal(IEnumerable<ResultDetail> resultDetails)
    {
        if (!resultDetails.Any())
        {
            return;
        }

        var matches = resultDetails.GroupBy(r => r.MatchId);

        foreach (var match in matches)
        {
            var criteria = new InsertProviderExternalResultDetailCriteria
            {
                MatchId = match.Key,
                Resources = CreateResources([.. match])
            };

            await repository.ExecuteAsync(criteria);
        }
    }

    private string CreateResources(List<ResultDetail> selectedResources)
    {
        var resources = new Resources<ResultDetail>
        {
            ResourceItems = selectedResources
        };

        return xmlConvertService.Serialize(resources);
    }

    private static IEnumerable<ResultDetail> IgnoreResult(IEnumerable<ResultDetail> resultDetails)
    {
        return resultDetails.Where(r =>
            !IgnoreResultByCondition.Conditions.TryGetValue(r.SportType, out var condition) || condition(r));
    }

    public async Task<List<Match>> GetMatchDetails(List<Match> matches)
    {
        var matchIds = matches.Select(m => m.MatchId).Distinct().ToList();
        if (!matchIds.Any())
        {
            return [];
        }

        var matchDetails = await repository.FetchAsync<MatchDetail>(new GetMatchDetailCriteria { MatchIds = string.Join(',', matchIds) });
        var listMatch = matchDetails.ToList();
        foreach (var match in matches)
        {
            match.MatchDetail = listMatch.Find(m => m.MatchId == match.MatchId);
        }

        return matches.Where(m => m.MatchDetail != null).ToList();
    }
}