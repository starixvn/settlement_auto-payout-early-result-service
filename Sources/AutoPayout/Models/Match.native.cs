namespace AutoPayout.Models;

public class Match
{
    public int MatchId { get; set; }

    public int LeagueId { get; set; }

    public string Conditions { get; set; }

    public int ResourceType { get; set; }

    public int SportType { get; set; }

    public int EarlyRetryTime { get; set; }

    public int MainRetryTime { get; set; }

    public IEnumerable<ResultDetail> ResultDetails { get; set; }

    public string ResourceRefund { get; set; }

    public MatchDetail MatchDetail { get; set; }

    public string UId = Guid.NewGuid().ToString();

    public override bool Equals(object obj)
    {
        return obj is Match match
               && match.MatchId == MatchId
               && match.ResourceType == ResourceType;
    }

    public override int GetHashCode() => MatchId * ResourceType;
}