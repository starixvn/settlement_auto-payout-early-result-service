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
        var validatedMatches = await matchService.GetMatchDetails(matches);

        lock (lockObject)
        {
            var newMatches = validatedMatches.Except(PoolOfMatches).ToList();
            if (newMatches.Count != 0)
            {
                PoolOfMatches.AddRange(newMatches);
            }

            return newMatches;
        }
    }
}