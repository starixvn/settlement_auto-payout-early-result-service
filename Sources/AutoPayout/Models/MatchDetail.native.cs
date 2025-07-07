namespace AutoPayout.Models;

public class MatchDetail
{
    public int MatchId { get; set; }

    public int GameSession { get; set; }

    public int BestOfMap { get; set; }

    public byte AutoSetting { get; set; }

    public int OverTimeSession { get; set; }

    public IEnumerable<VoidBetInfo> VoidBetInfos { get; set; }

    public string BetStatus { get; set; }

    public int ParentID { get; set; }

    public int LeagueGroupId { get; set; }

    public float SessionTime { get; set; }

    public short ChildMatchType { get; set; }

    public short SportType { get; set; }

    public int ChildLeagueType { get; set; }

    public int? LeagueType { get; set; }
    public int? HalfTimeHomeScore { get; set; }

    public int? HalfTimeAwayScore { get; set; }

    public int? FinalHomeScore { get; set; }

    public int? FinalAwayScore { get; set; }
}