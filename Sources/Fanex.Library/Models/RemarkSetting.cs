namespace Fanex.Library.Models;

public class RemarkSetting
{
    private const int PendingPosition = 0;

    private const int HoldKeyPosition = 6;

    private const int UnderSettlePosition = 2;

    private const int MtdHoldPosition = 4;

    private const int UndecidedPosition = 1;

    public string SettlementStatus { get; set; }

    public bool IsPending => !string.IsNullOrWhiteSpace(SettlementStatus) && SettlementStatus[PendingPosition].Equals('1');

    public bool IsUndecided => !string.IsNullOrWhiteSpace(SettlementStatus) && SettlementStatus.Length > UndecidedPosition && SettlementStatus[UndecidedPosition].Equals('1');

    public bool IsUnderSettle => !string.IsNullOrWhiteSpace(SettlementStatus) && SettlementStatus.Length > UnderSettlePosition && SettlementStatus[UnderSettlePosition].Equals('1');

    public bool IsMtdHold => !string.IsNullOrWhiteSpace(SettlementStatus) && SettlementStatus.Length > MtdHoldPosition && SettlementStatus[MtdHoldPosition].Equals('1');

    public bool IsHoldKey => !string.IsNullOrWhiteSpace(SettlementStatus) && SettlementStatus.Length > HoldKeyPosition && SettlementStatus[HoldKeyPosition].Equals('1');
}