using System.Xml.Serialization;

namespace AutoPayout.Models;

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