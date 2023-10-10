using AutoSettlement.Models;
using Fanex.Caching;
using Fanex.Data.Repository;
using Fanex.Repository.Models;

namespace AutoSettlement.Services;

public interface IMatchService
{
    Task<List<Match>> GetMatches();
}

public class MatchService : IMatchService
{
    private readonly AppSettingsKey appSettingsKey;
    private readonly ICache cache;
    private readonly IDynamicRepository repository;

    public MatchService(AppSettingsKey appSettingsKey, ICache cache, IDynamicRepository repository)
    {
        this.appSettingsKey = appSettingsKey;
        this.cache = cache;
        this.repository = repository;
    }

    public async Task<List<Match>> GetMatches()
    {
        // var matches = (await matchResultDetailsService.GroupMatches(resultDetails)).ToList();
        var matches = new List<Match>();
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
}