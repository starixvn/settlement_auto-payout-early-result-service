using AutoMapper;
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
    public static void InitializeServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddSingleton<ICacheService, CacheService>();
        service.AddTransient<ScaleOutSettleService, ScaleOutSettleServiceImp>();
        service.AddTransient<GeneralService, GeneralServiceImp>();

        service.AddSingleton<IAutoService, AutoService>();
        service.AddSingleton<ProcessService, ProcessServiceImp>();
        service.AddTransient<Creator, CreatorImp>();
        service.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        service.AddSingleton<CreateConditionChainService, CreateConditionChainServiceImp>();
        service.AddSingleton<SettlementService, SettlementServiceImp>();
        service.AddSingleton<CreateQueryService, CreateQueryServiceImp>();
        service.AddSingleton<WriteLogService, WriteLogServiceImp>();
        service.AddSingleton<MatchPoolService, MatchPoolServiceImp>();
        service.AddSingleton<MatchService, MatchServiceImp>();
        service.AddSingleton<UpdateResultStatusService, UpdateResultStatusServiceImp>();
        service.AddSingleton<ResultDetailService, ResultDetailServiceImp>();
        service.AddTransient<ExtraResultService, OverTimeServiceImp>();
        service.AddSingleton<MatchDetailService, MatchDetailServiceImp>();
        service.AddTransient<VerifyMatchStatus>();
        service.AddTransient<VerifyRunningOdds>();
        service.AddTransient<VerifyConfirmScore>();
        service.AddTransient<VerifyTimeResult>();
        service.AddTransient<VerifyBeforePushProfile>();
        service.AddTransient<VerifyInningStatus>();
        service.AddTransient<VerifyFinalResult>();
        service.AddTransient<VerifyFinalByBestOfMap>();
        service.AddTransient<VerifyFinalWithExtraResult>();
        service.AddTransient<IgnoreVerify>();
        service.AddTransient<VerifyBasketBallResult>();
        service.AddTransient<VerifySettledRelateResult>();
        service.AddTransient<VerifyResultWithResourceId>();
        service.AddSingleton<BasketballHelperService, BasketballHelperServiceImp>();
        service.AddSingleton<InvokeExternalAPI, InvokeExternalAPIImp>();

        service.AddSingleton<BasketBallCashOut>();
        service.AddSingleton<BasketballExtraStep>();
        service.AddSingleton<AddResourceRefund>();
        service.AddSingleton<TennisInsResourceResult>();
        service.AddSingleton<InitExtraStepService>();
        service.AddSingleton<CreateExtraStepService, CreateExtraStepServiceImp>();

        service.AddSingleton<XmlConvertService, XmlConvertServiceImp>();
        service.AddSingleton<IDynamicRepository, DynamicRepository>();
    }

    public static void AddAutoMapper(this IServiceCollection service)
    {
        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(typeof(ScaleOutMappingProfile));
        });

        var mapper = mapperConfig.CreateMapper();
        service.AddSingleton(mapper);
    }
}