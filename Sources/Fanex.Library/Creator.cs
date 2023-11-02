using Fanex.Library.Conditions;
using Fanex.Library.Constants;

namespace Fanex.Library;

public interface Creator
{
    Condition Create(string id);
}

public class CreatorImp : Creator
{
    private readonly VerifyMatchStatus verifyMatchStatus;

    private readonly VerifyRunningOdds verifyRunningOdds;

    private readonly VerifyConfirmScore verifyConfirmScore;

    private readonly InitialCondition initialCondition;

    private readonly VerifyBeforePushProfile verifyBeforePushProfile;

    public CreatorImp(
        VerifyMatchStatus verifyMatchStatus,
        VerifyRunningOdds verifyRunningOdds,
        VerifyConfirmScore verifyConfirmScore,
        InitialCondition initialCondition,
        VerifyBeforePushProfile verifyBeforePushProfile)
    {
        this.verifyMatchStatus = verifyMatchStatus;
        this.verifyRunningOdds = verifyRunningOdds;
        this.verifyConfirmScore = verifyConfirmScore;
        this.initialCondition = initialCondition;
        this.verifyBeforePushProfile = verifyBeforePushProfile;
    }

    public Condition Create(string id)
    {
        return id switch
        {
            ConditionId.VerifyMatchStatus => verifyMatchStatus,
            ConditionId.VerifyRunningOdds => verifyRunningOdds,
            ConditionId.VerifyConfirmScore => verifyConfirmScore,
            ConditionId.VerifyBeforePush => verifyBeforePushProfile,
            _ => initialCondition
        };
    }
}