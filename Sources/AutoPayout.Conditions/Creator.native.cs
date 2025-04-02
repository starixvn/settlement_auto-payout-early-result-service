using AutoPayout.Conditions.Services;
using AutoPayout.Conditions.Services.Conditions;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPayout.Conditions;

public interface Creator
{
    ICondition Create(string id);
}

public class CreatorImp(IServiceProvider serviceProvider) : Creator
{
    public ICondition Create(string id)
    {
        using var scope = serviceProvider.CreateScope();
        var provider = scope.ServiceProvider;

        return id switch
        {
            ConditionId.VerifyMatchStatus => provider.GetRequiredService<VerifyMatchStatus>(),
            ConditionId.VerifyRunningOdds => provider.GetRequiredService<VerifyRunningOdds>(),
            ConditionId.VerifyConfirmScore => provider.GetRequiredService<VerifyConfirmScore>(),
            ConditionId.VerifyRelatedSettleResult => provider.GetRequiredService<VerifySettledRelateResult>(),
            ConditionId.VerifyEsTimeResult => provider.GetRequiredService<VerifyTimeResult>(),
            ConditionId.VerifyBeforePush => provider.GetRequiredService<VerifyBeforePushProfile>(),
            ConditionId.VerifyInningStatus => provider.GetRequiredService<VerifyInningStatus>(),
            ConditionId.VerifyBaseballFinalResult => provider.GetRequiredService<VerifyFinalWithExtraResult>(),
            ConditionId.VerifyFinalResultGameSession => provider.GetRequiredService<VerifyFinalResult>(),
            ConditionId.VerifyFinalResultByBestOfMap => provider.GetRequiredService<VerifyFinalByBestOfMap>(),
            ConditionId.VerifyBasketBallFinalResult => provider.GetRequiredService<VerifyBasketBallResult>(),
            ConditionId.VerifyWithResourceId => provider.GetRequiredService<VerifyResultWithResourceId>(),
            _ => provider.GetRequiredService<IgnoreVerify>(),
        };
    }
}