using Fanex.Repository.Models;

namespace AutoSettlement.Services;

public interface IMatchPoolService
{
    Task<List<Match>> GetNewMatches();
}

public class MatchPoolService : IMatchPoolService
{
    private readonly IMatchService matchService;

    public List<Match> PoolOfMatches { get; } = new();

    private readonly object lockObject = new();

    public MatchPoolService(IMatchService matchService)
    {
        this.matchService = matchService;
    }

    public void RemoveMatch(Match match)
    {
        lock (lockObject)
        {
            PoolOfMatches.RemoveAll(m => m.MatchId == match.MatchId);
        }
    }

    public async Task<List<Match>> GetNewMatches()
    {
        var matches = await matchService.GetMatches();

        lock (lockObject)
        {
            var newMatches = matches.Except(PoolOfMatches).ToList();
            PoolOfMatches.AddRange(newMatches);

            return newMatches;
        }
    }
}