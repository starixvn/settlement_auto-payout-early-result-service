namespace AutoPayout.EarlyResult.Criteria;
public class CashOutCriteria
{
    public CashOutCriteria(int matchId, string betStatus, int[] lsHalf)
    {
        MatchId = matchId;
        BetStatus = betStatus;
        LsHalf = lsHalf;
    }

    public int MatchId { get; set; }

    public string BetStatus { get; set; }

    public int[] LsHalf { get; set; }
}
