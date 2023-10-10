using Fanex.Data.Repository;

namespace AutoSettlement.Criteria;

public class GetMatchCriteria : CriteriaBase
{
    public int AutoGroup { get; set; }

    public override string GetSettingKey() => "";

    public override bool IsValid() => AutoGroup > 0;
}