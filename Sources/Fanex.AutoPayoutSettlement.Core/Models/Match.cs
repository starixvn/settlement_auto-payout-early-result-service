namespace Fanex.AutoPayoutSettlement.Core.Models;

public class Match
{
    public int MatchId { get; set; }

    public int LeagueId { get; set; }

    public string? Conditions { get; set; }

    public int ResourceType { get; set; }

    public int SportType { get; set; }

    public IEnumerable<ResultDetail> ResultDetails { get; set; } = null;

    public override bool Equals(object obj)
    {
        return obj is Match match
               && match.MatchId == MatchId
               && match.ResourceType == ResourceType;
    }

    public override int GetHashCode() => MatchId * ResourceType;
}