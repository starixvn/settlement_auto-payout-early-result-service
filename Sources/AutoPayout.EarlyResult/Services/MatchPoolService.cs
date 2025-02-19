namespace AutoPayout.EarlyResult.Services;

public interface MatchPoolService
{
    Task<List<Match>> GetNewMatches();

    void RemoveMatch(Match match);
}

public class MatchPoolServiceImp(MatchService matchService) : MatchPoolService
{
    public List<Match> PoolOfMatches { get; } = [];

    private readonly Lock lockObject = new();

    public void RemoveMatch(Match match)
    {
        lock (lockObject)
        {
            PoolOfMatches.RemoveAll(m => m.MatchId == match.MatchId && m.ResourceType == match.ResourceType);
        }
    }

    public async Task<List<Match>> GetNewMatches()
    {
        var matches = await matchService.GetMatches();
        var newMatches = matches.Except(PoolOfMatches).ToList();
        var newValidMatches = await matchService.GetMatchDetails(newMatches);

        if (newValidMatches.Any())
        {
            lock (lockObject)
            {
                PoolOfMatches.AddRange(newValidMatches);
            }
        }

        return newValidMatches;
    }
}