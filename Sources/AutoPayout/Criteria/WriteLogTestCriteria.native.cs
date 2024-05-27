using System.Text.Json.Serialization;

namespace AutoPayout.Criteria;
public class WriteLogTestCriteria : NonQueryCommand
{
    public string Resources { get; set; }

    public override bool IsValid() => !string.IsNullOrEmpty(Resources);

    public override string GetSettingKey() => "NAP_Set_AutoPayoutPrepareOS_Insert";
}


public class ResourcesLogTest(Match match, string resourceId, string remark)
{
    [JsonPropertyName("m")]
    public int MatchId { get; set; } = match.MatchId;

    [JsonPropertyName("r")]
    public string ResourceId { get; set; } = resourceId;

    [JsonPropertyName("s")]
    public int StatusId => string.IsNullOrEmpty(remark) ? 2 : 3;

    [JsonPropertyName("rm")]
    public string Remark { get; set; } = remark;

    [JsonPropertyName("u")]
    public string UId { get; set; } = match.UId;
}
