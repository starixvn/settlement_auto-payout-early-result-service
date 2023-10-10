using AutoSettlement.Models;
using Fanex.AutoSettlement.Service;
using Fanex.Data.SqlServer;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<AppSettingsKey>(context.Configuration);
        services.InitializeServices(context.Configuration);
        services.AddFanexSql(context.Configuration);
        WorkerInjection.StartLogSetting(context.Configuration);
        WorkerInjection.StartUmSetting(context.Configuration);
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();