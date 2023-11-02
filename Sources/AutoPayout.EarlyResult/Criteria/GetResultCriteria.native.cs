using Fanex.Data.Repository;

namespace AutoPayoutSettlement.Criteria;

public class GetResultCriteria : CriteriaBase
{
    public int AutoPayoutGroup { get; set; }

    public int ResourceType { get; set; }

    public override string GetSettingKey() => "NAP_Set_AutoPayoutOS_ScheduleSel";

    public override bool IsValid() => AutoPayoutGroup > 0;
}