namespace AutoSettlement.Models;

public class AppSettingsKey
{
    public int RetryCount { get; set; }

    public int RetryInterval { get; set; }

    public int RetryValidate { get; set; }

    public string EnvironmentName { get; set; }

    public string CacheKey { get; set; }

    public List<string> SiteDbServer { get; set; }
}