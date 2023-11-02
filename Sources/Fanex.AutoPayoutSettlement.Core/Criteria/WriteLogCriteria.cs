using Fanex.Data.Repository;

namespace Fanex.AutoPayoutSettlement.Core.Criteria;

public class WriteLogCriteria : NonQueryCommand
{
    public WriteLogCriteria(int matchId, string errorMessage)
    {
        MatchId = matchId;
        Remark = errorMessage;
    }

    public int UserId { get; set; }

    public int MatchId { get; set; }

    public string Remark { get; set; }

    public string Ip { get; } = string.Empty;

    public int ActionId { get; } = 100;

    public override bool IsValid() => MatchId > 0;

    public override string GetSettingKey() => "WriteLog_UserLog_Settle";
}