using AutoPayout.Conditions.Services;

namespace AutoPayout.Conditions;

public interface Creator
{
    ICondition Create(string id);
}

public class CreatorImp : Creator
{
    private readonly VerifyMatchStatus verifyMatchStatus;

    private readonly VerifyRunningOdds verifyRunningOdds;

    private readonly VerifyConfirmScore verifyConfirmScore;

    private readonly VerifyBeforePushProfile verifyBeforePushProfile;

    public CreatorImp(
        VerifyMatchStatus verifyMatchStatus,
        VerifyRunningOdds verifyRunningOdds,
        VerifyConfirmScore verifyConfirmScore,
        VerifyBeforePushProfile verifyBeforePushProfile)
    {
        this.verifyMatchStatus = verifyMatchStatus;
        this.verifyRunningOdds = verifyRunningOdds;
        this.verifyConfirmScore = verifyConfirmScore;
        this.verifyBeforePushProfile = verifyBeforePushProfile;
    }

    public ICondition Create(string id)
    {
        return id switch
        {
            ConditionId.VerifyMatchStatus => verifyMatchStatus,
            ConditionId.VerifyRunningOdds => verifyRunningOdds,
            ConditionId.VerifyConfirmScore => verifyConfirmScore,
            ConditionId.VerifyBeforePush => verifyBeforePushProfile,
            _ => throw new NotImplementedException()
        };
    }
}