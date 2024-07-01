namespace AutoPayout.Models;

public class AppSettingsKey
{
    public int AutoPayoutGroup { get; set; }

    public int ResourceType { get; set; }

    public int RetryCount { get; set; }

    public int RetryInterval { get; set; }

    public int RetryValidate { get; set; }

    public int RetryProcessScore { get; set; }

    public string EnvironmentName { get; set; }

    public string CacheKey { get; set; }

    public int GroupTest { get; set; }

    public List<string> SiteDbServer { get; set; }

    public ThirdPartySettingKeys ThirdPartyUri { get; set; }
}


public class ThirdPartySettingKeys
{
    public string JackTimeTicket { get; set; }

    public string JackTimeResult { get; set; }
}