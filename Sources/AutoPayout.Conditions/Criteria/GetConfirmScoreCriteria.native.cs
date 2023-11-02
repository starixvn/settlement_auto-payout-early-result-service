namespace AutoPayout.Conditions.Criteria;

public class GetConfirmScoreCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public int SportType { get; set; }

    public string ResourceIds { get; set; }

    public override bool IsValid() => MatchId > 0;

    public override string GetSettingKey() => "NAP_Set_AutoPayoutOS_ResultSel";
}