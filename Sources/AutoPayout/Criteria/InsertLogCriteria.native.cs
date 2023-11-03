namespace AutoPayout.Criteria;

internal class InsertLogCriteria : NonQueryCommand
{
    public InsertLogCriteria(SettleCriteria criteria, int sportType)
    {
        MatchId = criteria.MatchId;
        Action = criteria.Action;
        SportType = sportType;
        AdminId = criteria.AdminId;
        SelectedResources = criteria.SelectedResources;
    }

    public int MatchId { get; set; }

    public string Action { get; set; }

    public int SportType { get; set; }

    public bool IsAuto { get; set; } = true;

    public int AdminId { get; }

    public string SelectedResources { get; set; }

    public override string GetSettingKey() => "NAP_SettlementLog_Ins";

    public override bool IsValid()
    {
        return MatchId > 0;
    }
}