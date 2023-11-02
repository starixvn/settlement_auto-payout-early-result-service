namespace Fanex.Library.Models;

public class MatchRemark
{
    private const int ToBase = 2;

    public byte SettlementStatus { get; set; }

    public string EventStatus { get; set; }

    public RemarkSetting Remark => new()
    {
        SettlementStatus = Convert.ToString(SettlementStatus, ToBase).Reverse().ToString()
    };
}