namespace AutoPayout.Models;

public class MatchDetail
{
    public int matchId { get; set; }

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
}