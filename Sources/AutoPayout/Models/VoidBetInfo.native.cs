using System.Text.Json.Serialization;

namespace AutoPayout.Models;
public class VoidBetInfo
{
    public string TransId { get; set; }

    public string CustId { get; set; }

    [JsonIgnore]
    public string Action { get; set; }
}
