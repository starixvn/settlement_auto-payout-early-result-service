using System.Xml.Serialization;

namespace AutoPayout.Models;

[XmlRoot("Resources")]
public class Resources<T>
{
    [XmlElement("Resource")]
    public List<T> ResourceItems { get; set; }
}