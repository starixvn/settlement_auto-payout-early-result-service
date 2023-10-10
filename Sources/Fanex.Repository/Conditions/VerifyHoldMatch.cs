using Fanex.Data.Repository;
using Fanex.Repository.Criteria;
using Fanex.Repository.Exceptions;
using Fanex.Repository.Models;

namespace Fanex.Repository.Conditions;

public class VerifyHoldMatch : Condition
{
    private readonly IDynamicRepository _repository;

    public VerifyHoldMatch(IDynamicRepository repository)
    {
        _repository = repository;
    }

    public override async Task HandleCondition(int matchId)
    {
        var criteria = new MatchRemarkCriteria { MatchId = matchId };
        var (matchStatus, _) = await _repository.FetchMultipleAsync<MatchRemark, object>(criteria);
        var match = matchStatus.First();

        if (match is not null)
        {
            VerifyMatch(match.Remark, matchId);
        }
        else
        {
            throw new HoldStatusException();
        }
    }

    private void VerifyMatch(RemarkSetting remark, int matchId)
    {
        bool[] conditionsToCheck = { remark.IsMtdHold, remark.IsUndecided, remark.IsPending, remark.IsUnderSettle, remark.IsHoldKey };
        if (conditionsToCheck.Any(item => item))
        {
            throw new HoldStatusException();
        }

        NextCondition?.HandleCondition(matchId);
    }
}