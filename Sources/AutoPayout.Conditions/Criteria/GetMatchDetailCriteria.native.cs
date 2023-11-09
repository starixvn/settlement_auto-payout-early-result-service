namespace AutoPayout.Conditions.Criteria;

public class GetMatchDetailCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public override string GetSettingKey() => "NAP_Set_MatchSelInfo";

    public override bool IsValid() => MatchId > 0;
}