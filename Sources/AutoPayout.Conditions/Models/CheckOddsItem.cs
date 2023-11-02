using System.Xml.Serialization;

namespace AutoPayout.Conditions.Models;

public class CheckOddsItem
{
    private readonly List<ResourceItem> resourceIdItems = new();

    [XmlAttribute("MatchId")]
    public int MatchId { get; set; }

    [XmlElement("ResourceId")]
    public List<ResourceItem> ResourceIdItems
    {
        get
        {
            ResourceIds.ForEach(item =>
            {
                resourceIdItems.Add(new ResourceItem(item));
            });
            return resourceIdItems;
        }
    }

    [XmlIgnore]
    public List<string> ResourceIds { get; set; }
}