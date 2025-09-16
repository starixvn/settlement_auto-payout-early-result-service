using AutoPayout.Constants;
using System.Xml.Serialization;

namespace AutoPayout.Models;

public class ResultDetail
{
    [XmlIgnore]
    public int MatchId { get; set; }

    [XmlIgnore]
    public int SportType { get; set; }

    [XmlIgnore]
    public int LeagueId { get; set; }

    [XmlAttribute("Id")]
    public string ResourceId { get; set; }

    [XmlAttribute("Home")]
    public string HomeValue { get; set; }

    [XmlAttribute("Away")]
    public string AwayValue { get; set; }

    [XmlAttribute("OtherInfo")]
    public string OtherInfo { get; set; }

    [XmlIgnore]
    public int ResourceType { get; set; }

    [XmlIgnore]
    public int ResourceStatus { get; set; }

    [XmlIgnore]
    public bool IsValid { get; set; } = true;

    [XmlIgnore]
    public bool Expired { get; set; }

    [XmlIgnore]
    public bool NeedRetry { get; set; }

    [XmlIgnore]
    public byte ResultStatusId { get; set; }

    [XmlIgnore]
    public short SettlementStatusId { get; set; }

    [XmlIgnore]
    public int GameSession { get; set; }

    [XmlIgnore]
    public int IntervalNo { get; set; }

    [XmlIgnore]
    public string RelatedInfo { get; set; }

    [XmlIgnore]
    public IEnumerable<ExternalSourceInfo> ExternalSourceInfo { get; set; } = [];

    [XmlIgnore]
    public IEnumerable<Score> Scores { get; set; } = [];

    [XmlIgnore]
    public byte ResultType => ResourceIds.TimePattern.Any(ResourceId.StartsWith) ? ResultTypes.Time : default;

    [XmlIgnore]
    public int EarlyRetryTime { get; set; }

    [XmlIgnore]
    public int MainRetryTime { get; set; }

    [XmlIgnore]
    public string RelatedCalResources { get; set; }

    [XmlIgnore]
    public List<string> RelatedCalResourceIds => string.IsNullOrEmpty(RelatedCalResources) ? [] : [.. RelatedCalResources.Split(",")];

    [XmlIgnore]
    public string RelatedSettResources { get; set; }

    [XmlIgnore]
    public List<string> RelatedSettResourcesIds => string.IsNullOrEmpty(RelatedSettResources) ? [] : [.. RelatedSettResources.Split(",")];

    public override string ToString() => $"{ResourceId}({HomeValue}-{AwayValue})";

    public override bool Equals(object obj) => obj is ResultDetail detail
               && detail.ResourceId == ResourceId
               && detail.MatchId == MatchId;

    public override int GetHashCode() => HashCode.Combine(MatchId, ResourceId);
}