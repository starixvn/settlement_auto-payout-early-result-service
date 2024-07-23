using AutoPayout.Constants;

namespace AutoPayout.Conditions.Models;

public class Score
{
    private string _awayValue = string.Empty;
    public int MatchId { get; set; }

    public int SourceId { get; set; }

    public string SourceName { get; set; }

    public string ResourceId { get; set; }

    public string HomeValue { get; set; }

    public string AwayValue
    {
        get => _awayValue;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                _awayValue = value;
            }
        }
    }

    public DateTime CreateDate { get; set; }

    public byte ResultType => ResourceIds.TimePattern.Any(x => ResourceId.StartsWith(x)) ? ResultTypes.Time : default;
}