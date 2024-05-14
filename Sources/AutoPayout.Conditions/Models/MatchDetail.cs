namespace AutoPayout.Conditions.Models;

public class MatchDetail
{
    public int GameSession { get; set; }

    public int BestOfMap { get; set; }

    public int OverTimeSession { get; set; }
    
    public byte AutoSetting { get; set; }
}