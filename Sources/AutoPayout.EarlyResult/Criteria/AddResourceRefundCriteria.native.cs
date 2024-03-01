using Fanex.Data.Repository;

namespace AutoPayout.EarlyResult.Criteria;
public class AddResourceRefundCriteria : INonQueryCommand
{
    public int MatchId { get; set; }

    public string ResourceIds { get; set; }

    public string GetSettingKey() => "NAP_Set_AutoRefundOS_ResultIns";

    public bool IsValid() => MatchId > 0;
}
