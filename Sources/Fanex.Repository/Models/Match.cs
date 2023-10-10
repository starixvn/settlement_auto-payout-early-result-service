namespace Fanex.Repository.Models;

public class Match
{
    public int MatchId { get; set; }

    public int LeagueId { get; set; }

    public string Conditions { get; set; }

    public string Results { get; set; }
}