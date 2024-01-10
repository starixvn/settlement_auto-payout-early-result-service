using Fanex.Data.Repository;

namespace AutoPayout.EarlyResult.Criteria;
public class InsRefundResultOSCriteria : INonQueryCommand
{
    public int MatchId { get; set; }

    public string GetSettingKey() => "NAP_Set_AutoRefundResultOS_Ins";

    public bool IsValid() => MatchId > 0;
}