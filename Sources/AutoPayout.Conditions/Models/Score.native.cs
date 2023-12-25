namespace AutoPayout.Conditions.Models;

public class Score
{
    public int MatchId { get; set; }

    public int SourceId { get; set; }

    public string SourceName { get; set; }

    public string ResourceId { get; set; }

    public string HomeValue { get; set; }

    public string AwayValue { get; set; }

    public DateTime CreateDate { get; set; }
}