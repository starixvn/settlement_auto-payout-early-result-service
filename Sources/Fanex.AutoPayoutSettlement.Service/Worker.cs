using AutoPayoutSettlement.Services;
using Fanex.Logging;
using Fanex.Logging.Extensions;
using Fanex.Logging.Extensions.Database;

namespace Fanex.AutoPayoutSettlement.Service;

public class Worker : BackgroundService
{
    private readonly IAutoService _autoService;
    private readonly IConfiguration configuration;
    private readonly IMaintenanceService maintenanceService;
    private int counter;

    public Worker(IAutoService autoService, IConfiguration configuration, IMaintenanceService maintenanceService)
    {
        _autoService = autoService;
        this.configuration = configuration;
        this.maintenanceService = maintenanceService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(configuration.GetValue<int>("ServiceInterval")));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await HandleAutoAsync();
        }
    }

    private async Task HandleAutoAsync()
    {
        try
        {
            if (!maintenanceService.IsMaintenance)
            {
                await _autoService.Handle();
                HandleGarbageCollector();
            }
        }
        catch (Exception exception)
        {
            var logMessage = new LogMessage(exception.Message)
                .AddSection("Database Info", DatabaseInfoGetters.GetDbExceptionInfo(exception))
                .AddSection("Server Info", CommonInfoGetters.GetServerInfo())
                .AddSection("Exception Info", CommonInfoGetters.GetExceptionInfo(exception))
                .ToString();
            LogService.Error(logMessage);
        }
    }

    private void HandleGarbageCollector()
    {
        if (counter >= configuration.GetValue<int>("GarbageCollectorInterval"))
        {
            counter = 0;
            GC.WaitForPendingFinalizers();
        }

        counter += configuration.GetValue<int>("ServiceInterval");
    }
}