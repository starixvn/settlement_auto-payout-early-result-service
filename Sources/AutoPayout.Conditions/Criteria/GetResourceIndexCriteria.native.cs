namespace AutoPayout.Conditions.Criteria;
public class GetResourceIndexCriteria(int sportType) : CriteriaBase
{
    public int SportType { get; set; } = sportType; //NOSONAR

    public override string GetSettingKey() => "NAP_Admin_MajorRound_Sel";

    public override bool IsValid() => SportType > 0;
}
