using System.Xml.Serialization;

namespace Fanex.AutoPayoutSettlement.Core.Models;

[XmlRoot("Resources")]
public class Resources<T>
{
    [XmlElement("Resource")]
    public List<T> ResourceItems { get; set; }
}