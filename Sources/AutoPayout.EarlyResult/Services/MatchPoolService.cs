namespace AutoPayout.EarlyResult.Services;

public interface MatchPoolService
{
    Task<List<Match>> GetNewMatches();

    void RemoveMatch(Match match);
}

public class MatchPoolServiceImp(MatchService matchService) : MatchPoolService
{
    public List<Match> PoolOfMatches { get; } = new(); //NOSONAR

    private readonly object lockObject = new();  //NOSONAR

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

        lock (lockObject)
        {
            var newMatches = matches.Except(PoolOfMatches).ToList();
            PoolOfMatches.AddRange(newMatches);

            return newMatches;
        }
    }
}