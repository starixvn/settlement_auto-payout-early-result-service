namespace AutoPayout.Conditions.Criteria;

public class GetPlayerInfoCriteria : CriteriaBase
{
    public int MatchId { get; set; }
    public int SportType { get; set; }
    public string ListResourceIds { get; set; } = string.Empty;

    public override string GetSettingKey() => "NAP_Set_PlayerInfoSel";

    public override bool IsValid() => true;
}
