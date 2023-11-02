using Fanex.AutoPayoutSettlement.Core.Models;
using Fanex.Data.Repository;
using Fanex.Library.Constants;
using Fanex.Library.Criteria;
using Fanex.Library.Exceptions;
using Fanex.Library.Models;

namespace Fanex.Library.Conditions;

public class VerifyMatchStatus : Condition
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
        var matchStatusValue = matchStatus.First();

        if (matchStatusValue is not null)
        {
            VerifyMatch(matchStatusValue);
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