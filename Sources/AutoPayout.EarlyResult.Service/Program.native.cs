using AutoPayout.EarlyResult.Service;
using AutoPayout.Models;
using Fanex.Data.SqlServer;
using Fanex.Logging.Sentry.RavenClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ONEbook.UM.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.InitializeServices(builder.Configuration);

builder.Services.Configure<AppSettingsKey>(builder.Configuration);
builder.Services.AddOneBookUm(builder.Configuration);
builder.Services.AddFanexLogSentry(builder.Configuration);
builder.Services.AddFanexSql(builder.Configuration);

builder.Services.AddHostedService<Worker>();

builder.Services.AddHttpClient();

var app = builder.Build();

app.Run(async context =>
{
    await context.Response.WriteAsync("OK");
});

app.Run();