namespace AutoPayout.Conditions.Criteria;

public class GetMainResultDetailCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public int SportType { get; set; }

    public override bool IsValid() => true;

    public override string GetSettingKey() => "NAP_Set_AutoPayout_ResultDetailByMatchIdSel_OS";
}