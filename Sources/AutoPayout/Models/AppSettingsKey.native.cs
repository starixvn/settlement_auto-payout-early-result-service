namespace AutoPayout.Models;

public class AppSettingsKey
#pragma warning restore VSSpell001
{
    public int AutoPayoutGroup { get; set; }

    public int ResourceType { get; set; }

    public int RetryCount { get; set; }

    public int RetryInterval { get; set; }

    public int RetryValidate { get; set; }

    public int RetryProcessScore { get; set; }

    public string EnvironmentName { get; set; }

    public string CacheKey { get; set; }

    public List<string> SiteDbServer { get; set; }
}