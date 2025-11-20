using AutoPayout.EarlyResult.Service;
using AutoPayout.Models;
using Fanex.Caching.Redis;
using Fanex.Data.SqlServer;
using Fanex.Logging.Sentry.RavenClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ONEbook.UM.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddRedisCache(builder.Configuration.GetSection("Fanex.Caching.Redis"));

services.AddOneBookUmForService(builder.Configuration);

services.AddFanexLogSentry(builder.Configuration);

services.AddFanexSql(builder.Configuration);

services.InitializeServices(builder.Configuration);

services.AddMassTransit(builder.Configuration);

services.AddAutoMapper();

services.Configure<AppSettingsKey>(builder.Configuration);

services.AddHostedService<Worker>();

services.AddHttpClient();

services.AddHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/healthz");

await app.RunAsync();