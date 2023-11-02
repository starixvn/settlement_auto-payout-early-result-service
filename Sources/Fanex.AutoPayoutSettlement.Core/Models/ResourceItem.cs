using System.Xml.Serialization;

namespace Fanex.AutoPayoutSettlement.Core.Models;

public class ResourceItem
{
    public ResourceItem()
    {
    }

    public ResourceItem(string id)
    {
        ResourceId = id;
    }

    [XmlAttribute("Id")]
    public string ResourceId { get; set; }
}