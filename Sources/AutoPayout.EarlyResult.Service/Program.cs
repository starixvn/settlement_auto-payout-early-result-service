using AutoPayout.EarlyResult.Service;
using AutoPayoutSettlement.Models;
using Fanex.Data.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFanexSql(builder.Configuration);

builder.Services.InitializeServices(builder.Configuration);

builder.Services.Configure<AppSettingsKey>(builder.Configuration);

builder.Services.StartUmSetting(builder.Configuration);

builder.Services.AddHostedService<Worker>();

var app = builder.Build();

app.Run(async context =>
{
    await context.Response.WriteAsync("OK");
});

app.Run();