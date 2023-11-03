using Fanex.Data.Repository;

namespace AutoPayout.EarlyResult.Criteria;

public class GetHedgeMatchCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public override string GetSettingKey() => "NAP_Set_MatchSelHedge";

    public override bool IsValid() => MatchId > 0;
}