using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPayout.EarlyResult.Service;

public static class MassTransitConfig
{
    public static void AddMassTransit(this IServiceCollection service, IConfiguration configuration)
    {
        var host = configuration.GetValue("RabbitMQ:Host", string.Empty);
        var virtualHost = configuration.GetValue("RabbitMQ:VirtualHost", string.Empty);
        var username = configuration.GetValue("RabbitMQ:Username", string.Empty);
        var password = configuration.GetValue("RabbitMQ:Password", string.Empty);

        service.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(host, virtualHost, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.UseMessageRetry(retryConfig =>
                {
                    retryConfig.Interval(1, TimeSpan.FromSeconds(2));
                });

                cfg.ConfigureEndpoints(context);
            });
        });
    }
}