using Newtonsoft.Json;

namespace AutoPayout.Conditions.Models;


public class OddFlexModel
{
    public string ResourceId { get; set; }
    public int BetType { get; set; }
    public string BetChoice { get; set; }
    public string OddsInfo { get; set; }

    public OddsInfoModel Info => JsonConvert.DeserializeObject<OddsInfoModel>(OddsInfo);

    public override bool Equals(object obj)
        => obj is OddFlexModel model
           && model.ResourceId == ResourceId
           && model.OddsInfo == OddsInfo;

    public override int GetHashCode() => HashCode.Combine(ResourceId, OddsInfo);
}

public class OddsInfoModel
{
    [JsonProperty("pid")]
    public int PlayerId { get; set; }

    [JsonProperty("pname")]
    public string PlayerName { get; set; }

    [JsonProperty("tid")]
    public int TeamId { get; set; }

    [JsonProperty("isH")]
    public bool IsHome { get; set; }

    [JsonProperty("s")]
    public List<Statistic> Statistics { get; set; } = [];
    [JsonProperty("r")]
    public List<ResultInfo> Results { get; set; } = [];
}

public class Statistic
{
    [JsonProperty("t")]
    public string Type { get; set; }

    [JsonProperty("value")]
    public string Value { get; set; }
}

public class ResultInfo
{
    [JsonProperty("t")]
    public string Type { get; set; }

    [JsonProperty("value")]
    public string Value { get; set; }
}


