namespace AutoPayout.Criteria;

public class WriteLogCriteria(int matchId, string errorMessage) : NonQueryCommand
{
    public int UserId { get; set; }

    public int MatchId { get; set; } = matchId;

    public string Remark { get; set; } = errorMessage;

    public string Ip { get; } = string.Empty;

    public int ActionId { get; } = 100;

    public override bool IsValid() => MatchId > 0;

    public override string GetSettingKey() => "WriteLog_UserLog_Settle";
}