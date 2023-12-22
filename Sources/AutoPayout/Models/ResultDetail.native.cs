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

    [XmlIgnore]
    public int ResourceType { get; set; }

    [XmlIgnore]
    public int ResourceStatus { get; set; }

    [XmlIgnore]
    public bool IsValid { get; set; } = true;

    [XmlIgnore]
    public bool Expired { get; set; } = false;

    [XmlIgnore]
    public byte ResultStatusId { get; set; }

    [XmlIgnore]
    public IEnumerable<ExternalSourceInfo> ExternalSourceInfo { get; set; }
}