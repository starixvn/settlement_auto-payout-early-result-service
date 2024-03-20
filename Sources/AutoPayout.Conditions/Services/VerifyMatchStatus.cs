namespace AutoPayout.Conditions.Services;

public class VerifyMatchStatus(IDynamicRepository repository) : ICondition
{
    public async Task HandleCondition(Match match)
    {
        var criteria = new MatchRemarkCriteria { MatchId = match.MatchId };
        var (matchStatus, _) = await repository.FetchMultipleAsync<MatchRemark, object>(criteria);

        var matchRemarks = matchStatus.ToList();
        if (matchRemarks.Any())
        {
            VerifyMatch(matchRemarks[0], match.ResourceType);
        }
        else
        {
            throw new HoldStatusException();
        }
    }

    private static void VerifyMatch(MatchRemark matchRemark, int resourceType)
    {
        var remark = matchRemark.Remark;
        List<bool> conditionsToCheck = new() { remark.IsMtdHold, remark.IsUndecided, remark.IsPending, remark.IsUnderSettle, remark.IsHoldKey };
        if (conditionsToCheck.Exists(item => item))
        {
            throw new HoldStatusException();
        }

        if (resourceType == ResourceType.MainResource && matchRemark.EventStatus.ToLowerInvariant() != EventStatus.Running)
        {
            throw new InvalidMatchStatusException();
        }
    }
}