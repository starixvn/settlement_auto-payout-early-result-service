using AutoPayout.Conditions.Services;

namespace AutoPayout.Conditions;

public interface Creator
{
    ICondition Create(string id);
}

public class CreatorImp(VerifyMatchStatus verifyMatchStatus,
        VerifyRunningOdds verifyRunningOdds,
        VerifyConfirmScore verifyConfirmScore,
        VerifyBeforePushProfile verifyBeforePushProfile,
        VerifyInningStatus verifyInningStatus,
        VerifyFinalResult verifyFinalResult,
        VerifyFinalByBestOfMatch verifyFinalByBestOfMatch
       )
    : Creator
{
    public ICondition Create(string id)
    {
        return id switch
        {
            ConditionId.VerifyMatchStatus => verifyMatchStatus,
            ConditionId.VerifyRunningOdds => verifyRunningOdds,
            ConditionId.VerifyConfirmScore => verifyConfirmScore,
            ConditionId.VerifyBeforePush => verifyBeforePushProfile,
            ConditionId.VerifyInningStatus => verifyInningStatus,
            ConditionId.VerifyFinalResult => verifyFinalResult,
            ConditionId.VerifyFinalResultByBestOfMatch => verifyFinalByBestOfMatch,
            _ => throw new NotImplementedException()
        };
    }
}