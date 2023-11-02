using Fanex.Data.Repository;

namespace Fanex.Library.Criteria;

public class MatchRemarkCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public override bool IsValid() => MatchId > 0;

    public override string GetSettingKey() => "NAP_Set_AutoPayout_MatchSelDetail";
}