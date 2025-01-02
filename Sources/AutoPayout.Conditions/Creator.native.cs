using AutoPayout.Conditions.Services;
using AutoPayout.Conditions.Services.Conditions;

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
        VerifyFinalByBestOfMap verifyFinalByBestOfMap,
        VerifyTimeResult verifyTimeResult,
        VerifyBasketBallResult verifyBasketBallFinalResult,
        VerifyFinalWithExtraResult verifyBaseballFinalResult,
        VerifySettledRelateResult verifySettledRelateResult,
        VerifyResultWithResourceId verifyResultWithResourceId,
        IgnoreVerify ignoreVerify
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
            ConditionId.VerifyRelatedSettleResult => verifySettledRelateResult,
            ConditionId.VerifyEsTimeResult => verifyTimeResult,
            ConditionId.VerifyBeforePush => verifyBeforePushProfile,
            ConditionId.VerifyInningStatus => verifyInningStatus,
            ConditionId.VerifyBaseballFinalResult => verifyBaseballFinalResult,
            ConditionId.VerifyFinalResultGameSession => verifyFinalResult,
            ConditionId.VerifyFinalResultByBestOfMap => verifyFinalByBestOfMap,
            ConditionId.VerifyBasketBallFinalResult => verifyBasketBallFinalResult,
            ConditionId.VerifyWithResourceId => verifyResultWithResourceId,
            _ => ignoreVerify,
        };
    }
}