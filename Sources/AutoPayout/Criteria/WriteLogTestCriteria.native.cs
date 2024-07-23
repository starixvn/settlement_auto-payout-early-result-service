using AutoPayout.Helpers;
using Newtonsoft.Json;

namespace AutoPayout.Criteria;
public class WriteLogTestCriteria : NonQueryCommand
{
    public string Resources { get; set; }

    public override bool IsValid() => !string.IsNullOrEmpty(Resources);

    public override string GetSettingKey() => "NAP_Set_AutoPayoutPrepareOS_Insert";
}


public class ResourcesLogTest
{
    private readonly List<string> remarks = new();
    public ResourcesLogTest(Match match, ResultDetail resultDetail, string remark)
    {
        MatchId = match.MatchId;
        ResourceId = resultDetail.ResourceId;
        remarks.Add(remark);
        remarks.AddIf(!string.IsNullOrEmpty(resultDetail.HomeValue)
                      && !string.IsNullOrEmpty(resultDetail.HomeValue),
            $"Score: {resultDetail.HomeValue}-{resultDetail.AwayValue}");
        remarks.AddIf(resultDetail.ExternalSourceInfo.Any(), string.Join(", ", resultDetail.ExternalSourceInfo.Select(e => $"{e.SourceName ?? "Trader"}")));
        UId = match.UId;
        StatusId = string.IsNullOrEmpty(remark) ? 2 : 3;
    }

    [JsonProperty(PropertyName = "m")]
    public int MatchId { get; set; }

    [JsonProperty(PropertyName = "r")]
    public string ResourceId { get; set; }

    [JsonProperty(PropertyName = "s")]
    public int StatusId { get; set; }

    [JsonProperty(PropertyName = "rm")]
    public string Remark => string.Join(", ", remarks.Where(r => !string.IsNullOrEmpty(r)));

    [JsonProperty(PropertyName = "u")]
    public string UId { get; set; }
}
