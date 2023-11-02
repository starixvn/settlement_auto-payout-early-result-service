using AutoMapper;
using AutoPayoutSettlement.Services;
using Fanex.AutoPayoutSettlement.Core.Services;
using Fanex.Caching;
using Fanex.Caching.Redis;
using Fanex.Data.Repository;
using Fanex.Library;
using Fanex.Library.Conditions;
using Fanex.Library.Services;
using Fanex.Logging;
using Fanex.Logging.Sentry.RavenClient;
using Fanex.Settlement.Converter.Services;
using Fanex.Settlement.ScaleOut;
using Fanex.Settlement.ScaleOut.Services;
using Microsoft.AspNetCore.Http;
using ONEbook.UM;
using ONEbook.UM.AspNetCore;
using ONEbook.UM.Configurations;
using ONEbook.UM.Helpers;
using ONEbook.UM.Transport;
using SharpRaven;

namespace Fanex.AutoPayoutSettlement.Service;

public static class WorkerInjection
{
    public static void InitializeServices(this IServiceCollection service, IConfiguration configuration)
    {
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
        service.AddSingleton<ScoreService, ScoreServiceImp>();
        service.AddSingleton<VerifyMatchStatus>();
        service.AddSingleton<VerifyRunningOdds>();
        service.AddSingleton<VerifyConfirmScore>();
        service.AddSingleton<InitialCondition>();
        service.AddSingleton<VerifyBeforePushProfile>();

        service.AddSingleton<XmlConvertService, XmlConvertServiceImp>();
        service.AddSingleton<IMapper, Mapper>();
        service.AddSingleton<ScaleOutSettleService, ScaleOutSettleServiceImp>();
        service.AddSingleton<GeneralService, GeneralServiceImp>();
        service.AddSingleton<IDynamicRepository, DynamicRepository>();
        service.AddSingleton(new UMClientConfiguration().Initialize(configuration));
        service.AddSingleton<IUMCacheService, UMCacheService>();
        service.AddSingleton<IUMRestClient, UMRestClient>();
        service.AddSingleton<ONEbook.UM.Services.IMaintenanceService, ONEbook.UM.Services.MaintenanceService>();
        ConfigureRedis(service, configuration);
        ConfigureAutoMapper(service);
    }

    private static void ConfigureRedis(IServiceCollection service, IConfiguration configuration)
    {
        var redisHost = configuration.GetSection("Redis.Server").Value;
        var redisPort = configuration.GetSection("Redis.Port").Value;
        var redisPassword = configuration.GetSection("Redis.Password").Value;
        var redisDatabase = configuration.GetSection("Redis.DefaultDatabase").Value;
        service.AddSingleton<ICache>(new RedisCache(new RedisCacheOptions
        {
            ConnectionString = $"{redisHost}:{redisPort},password={redisPassword},defaultDatabase={redisDatabase}",
            ApplicationName = configuration.GetSection("Redis.ApplicationName").Value,
        }));
        service.AddSingleton<ICacheService>(new CacheService());
    }

    public static void StartUmSetting(this IServiceCollection service, IConfiguration configuration)
    {
        var umConfig = new UMClientConfiguration().Initialize(configuration);

        UMClientManager
            .UseConfig(umConfig)
            .UseCacheService(new UMCacheService(new CacheService()))
            .UseRestClient(new UMRestClient(umConfig));
    }

    public static void StartLogSetting(IConfiguration configuration)
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