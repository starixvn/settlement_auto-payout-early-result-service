namespace AutoPayout.Conditions.Services;

public class VerifyMatchStatus : ICondition
{
    private readonly IDynamicRepository repository;

    public VerifyMatchStatus(IDynamicRepository repository)
    {
        this.repository = repository;
    }

    public async Task HandleCondition(Match match)
    {
        var criteria = new MatchRemarkCriteria { MatchId = match.MatchId };
        var (matchStatus, _) = await repository.FetchMultipleAsync<MatchRemark, object>(criteria);

        if (matchStatus.Any())
        {
            VerifyMatch(matchStatus.First());
        }
        else
        {
            throw new HoldStatusException();
        }
    }

    private static void VerifyMatch(MatchRemark matchRemark)
    {
        var remark = matchRemark.Remark;
        List<bool> conditionsToCheck = new() { remark.IsMtdHold, remark.IsUndecided, remark.IsPending, remark.IsUnderSettle, remark.IsHoldKey };
        if (conditionsToCheck.Exists(item => item))
        {
            throw new HoldStatusException();
        }

        if (matchRemark.EventStatus.ToLowerInvariant() != EventStatus.Running)
        {
            throw new InvalidMatchStatusException();
        }
    }
}