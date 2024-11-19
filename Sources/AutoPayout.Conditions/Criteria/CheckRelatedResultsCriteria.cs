namespace AutoPayout.Conditions.Criteria;
public class CheckRelatedResultsCriteria : CriteriaBase
{
    public int MatchId { get; set; }
    public short SportType { get; set; }
    public string RelatedCalResources { get; set; }
    public string RelatedSettResources { get; set; }
    public override string GetSettingKey() => "NAP_Set_AutoPayout_ResultStatusSel";

    public override bool IsValid() => true;
}
