using AutoMapper;
using AutoPayout.Conditions;
using AutoPayout.Conditions.Services;
using AutoPayout.EarlyResult.Services;
using AutoPayout.EarlyResult.Services.ExtraStepService;
using AutoPayout.Services;
using Fanex.Caching;
using Fanex.Caching.Redis;
using Fanex.Data.Repository;
using Fanex.Logging;
using Fanex.Logging.Sentry.RavenClient;
using Fanex.Settlement.Converter.Services;
using Fanex.Settlement.ScaleOut;
using Fanex.Settlement.ScaleOut.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ONEbook.UM;
using ONEbook.UM.AspNetCore;
using ONEbook.UM.Configurations;
using ONEbook.UM.Helpers;
using ONEbook.UM.Transport;
using SharpRaven;

namespace AutoPayout.EarlyResult.Service;

public static class WorkerInjection
{
    public static void InitializeServices(this IServiceCollection service, IConfiguration configuration)
    {
        ConfigureAutoMapper(service);
        service.AddTransient<ScaleOutSettleService, ScaleOutSettleServiceImp>();
        service.AddTransient<GeneralService, GeneralServiceImp>();

        service.AddTransient<IMaintenanceService, MaintenanceServiceImp>();
        service.AddSingleton<IAutoService, AutoService>();
        service.AddSingleton<ProcessService, ProcessServiceImp>();
        service.AddSingleton<Creator, CreatorImp>();
        service.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        service.AddSingleton<CreateConditionChainService, CreateConditionChainServiceImp>();
        service.AddSingleton<SettlementService, SettlementServiceImp>();
        service.AddSingleton<CreateQueryService, CreateQueryServiceImp>();
        service.AddSingleton<WriteLogService, WriteLogServiceImp>();
        service.AddSingleton<MatchPoolService, MatchPoolServiceImp>();
        service.AddSingleton<MatchService, MatchServiceImp>();
        service.AddSingleton<UpdateResultStatusService, UpdateResultStatusServiceImp>();
        service.AddSingleton<ResultDetailService, ResultDetailServiceImp>();
        service.AddSingleton<VerifyMatchStatus>();
        service.AddSingleton<VerifyRunningOdds>();
        service.AddSingleton<VerifyConfirmScore>();
        service.AddSingleton<VerifyBeforePushProfile>();
        service.AddSingleton<VerifyInningStatus>();
        service.AddSingleton<VerifyFinalResult>();
        service.AddSingleton<VerifyFinalResult>();
        service.AddSingleton<VerifyFinalByBestOfMap>();

        service.AddSingleton<TennisExtraStep>();
        service.AddSingleton<InitExtraStepService>();
        service.AddSingleton<CreateExtraStepService, CreateExtraStepServiceImp>();

        service.AddSingleton<XmlConvertService, XmlConvertServiceImp>();
        service.AddSingleton<IDynamicRepository, DynamicRepository>();
        service.AddSingleton(new UMClientConfiguration().Initialize(configuration));
        service.AddSingleton<IUMCacheService, UMCacheService>();
        service.AddSingleton<IUMRestClient, UMRestClient>();
        service.AddSingleton<ONEbook.UM.Services.IMaintenanceService, ONEbook.UM.Services.MaintenanceService>();
        service.AddMemoryCache();
        ConfigureRedis(service, configuration);
        StartLogSetting(configuration);
    }

    private static void ConfigureRedis(IServiceCollection service, IConfiguration configuration)
    {
        service.AddSingleton<ICacheService, CacheService>();
        service.AddRedisCache(configuration.GetSection("Fanex.Caching.Redis"));
    }

    public static void StartUmSetting(this IServiceCollection service, IConfiguration configuration)
    {
        var umConfig = new UMClientConfiguration().Initialize(configuration);

        UMClientManager
            .UseConfig(umConfig)
            .UseCacheService(new UMCacheService(new CacheService()))
            .UseRestClient(new UMRestClient(umConfig));
    }

    private static void StartLogSetting(IConfiguration configuration)
    {
        var logEngine = new List<ILogEngine>();

        var sentryEngineOptions = new SentryEngineOptions
        {
            Dsn = new Dsn(configuration.GetSection("SentryToken").Value)
        };

        logEngine.Add(new SentryLogging(sentryEngineOptions));

        LogManager.SetDefaultLogCategory(configuration.GetSection("SiteCategory").Value).Use(logEngine);
    }

    private static void ConfigureAutoMapper(IServiceCollection service)
    {
        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(typeof(ScaleOutMappingProfile));
        });

        var mapper = mapperConfig.CreateMapper();
        service.AddSingleton(mapper);
    }
}