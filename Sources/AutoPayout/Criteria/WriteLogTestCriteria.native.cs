using System.Text.Json.Serialization;

namespace AutoPayout.Criteria;
public class WriteLogTestCriteria : NonQueryCommand
{
    public string Resources { get; set; }

    public override bool IsValid() => !string.IsNullOrEmpty(Resources);

    public override string GetSettingKey() => "NAP_Set_AutoPayoutPrepareOS_Insert";
}


public class ResourcesLogTest(Match match, string resourceId, RemarkLog remark)
{
    [JsonPropertyName("u")]
    public string UId { get; set; } = match.UId;

    [JsonPropertyName("m")]
    public int MatchId { get; set; } = match.MatchId;

    [JsonPropertyName("r")]
    public string ResourceId { get; set; } = resourceId;

    [JsonPropertyName("s")]
    public int StatusId => string.IsNullOrEmpty(remark.Remark) ? 2 : 3;

    [JsonPropertyName("rm")]
    public RemarkLog Remark { get; set; } = remark;
}

public class RemarkLog(string uId, string remark)
{
    public string UId { get; set; }

    public string Remark { get; set; }
}
