namespace AutoPayout.Criteria;

public class LogDirectlyCriteria : NonQueryCommand
{
    public int UserId { get; set; }
    public string? Doing { get; set; }
    public int Extend { get; set; }

    public override string GetSettingKey() => "NAP_UserLog_WriteLogPreSettlement";

    public override bool IsValid() => true;
}