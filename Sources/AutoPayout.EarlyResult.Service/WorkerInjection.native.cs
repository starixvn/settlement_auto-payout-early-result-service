using AutoPayout.Conditions;
using AutoPayout.Conditions.Services;
using AutoPayout.Conditions.Services.Conditions;
using AutoPayout.Conditions.Services.HelperService.GetAndGenerate;
using AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;
using AutoPayout.EarlyResult.Services;
using AutoPayout.EarlyResult.Services.ExtraStepService;
using AutoPayout.EarlyResult.Services.ExtraStepService.AfterSettle;
using AutoPayout.EarlyResult.Services.ExtraStepService.BeforeSettle;
using AutoPayout.Services;
using Fanex.Caching;
using Fanex.Data.Repository;
using Fanex.Settlement.Converter.Services;
using Fanex.Settlement.ScaleOut;
using Fanex.Settlement.ScaleOut.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPayout.EarlyResult.Service;

public static class WorkerInjection
{
    public static void InitializeServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICacheService, CacheService>();
        services.AddTransient<ScaleOutSettleService, ScaleOutSettleServiceImp>();
        services.AddTransient<GeneralService, GeneralServiceImp>();

        services.AddSingleton<IAutoService, AutoService>();
        services.AddSingleton<ProcessService, ProcessServiceImp>();
        services.AddTransient<Creator, CreatorImp>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.AddSingleton<CreateConditionChainService, CreateConditionChainServiceImp>();
        services.AddSingleton<SettlementService, SettlementServiceImp>();
        services.AddSingleton<CreateQueryService, CreateQueryServiceImp>();
        services.AddSingleton<WriteLogService, WriteLogServiceImp>();
        services.AddSingleton<MatchPoolService, MatchPoolServiceImp>();
        services.AddSingleton<MatchService, MatchServiceImp>();
        services.AddSingleton<UpdateResultStatusService, UpdateResultStatusServiceImp>();
        services.AddSingleton<ResultDetailService, ResultDetailServiceImp>();
        services.AddTransient<ExtraResultService, OverTimeServiceImp>();
        services.AddSingleton<MatchDetailService, MatchDetailServiceImp>();

        services.AddTransient<VerifyMatchStatus>();
        services.AddTransient<VerifyRunningOdds>();
        services.AddTransient<VerifyConfirmScore>();
        services.AddTransient<VerifyTimeResult>();
        services.AddTransient<VerifyBeforePushProfile>();
        services.AddTransient<VerifyInningStatus>();
        services.AddTransient<VerifyFinalResult>();
        services.AddTransient<VerifyFinalByBestOfMap>();
        services.AddTransient<VerifyFinalWithExtraResult>();
        services.AddTransient<IgnoreVerify>();
        services.AddTransient<VerifyBasketBallResult>();
        services.AddTransient<VerifySettledRelateResult>();
        services.AddTransient<VerifyResultWithResourceId>();
        services.AddTransient<VerifyFinalWithBetStatus>();

        services.AddSingleton<BasketballHelperService, BasketballHelperServiceImp>();
        services.AddSingleton<InvokeExternalAPI, InvokeExternalAPIImp>();

        services.AddSingleton<BasketBallCashOut>();
        services.AddSingleton<BasketballExtraStep>();
        services.AddSingleton<AddResourceRefund>();
        services.AddSingleton<TennisInsResourceResult>();
        services.AddSingleton<InitExtraStepService>();
        services.AddSingleton<CreateExtraStepService, CreateExtraStepServiceImp>();

        services.AddSingleton<XmlConvertService, XmlConvertServiceImp>();
        services.AddSingleton<IDynamicRepository, DynamicRepository>();
    }

    public static void AddAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddProfile<ScaleOutMappingProfile>());
    }
}