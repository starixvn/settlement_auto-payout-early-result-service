namespace AutoPayout.Conditions.Criteria;

public class GetResultDetailCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public string SettleType { get; set; }

    public int SportType { get; set; }

    public override string GetSettingKey() => "NAP_Set_MatchSelDetail_Resources";

    public override bool IsValid() => MatchId > 0;
}