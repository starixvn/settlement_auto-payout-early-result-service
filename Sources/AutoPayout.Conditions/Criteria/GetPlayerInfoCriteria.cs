namespace AutoPayout.Conditions.Criteria;

public class GetPlayerInfoCriteria : CriteriaBase
{
    public int MatchId { get; set; }
    public int SportType { get; set; }

    public override string GetSettingKey() => "NAP_Set_PlayerInfoSel";

    public override bool IsValid() => true;
}
