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
			ConditionId.VerifyEsTimeResult => verifyTimeResult,
			ConditionId.VerifyBeforePush => verifyBeforePushProfile,
			ConditionId.VerifyInningStatus => verifyInningStatus,
			ConditionId.VerifyBaseballFinalResult => verifyBaseballFinalResult,
			ConditionId.VerifyFinalResult => verifyFinalResult,
			ConditionId.VerifyFinalResultByBestOfMap => verifyFinalByBestOfMap,
			ConditionId.VerifyBasketBallFinalResult => verifyBasketBallFinalResult,
			_ => ignoreVerify,
		};
	}
}