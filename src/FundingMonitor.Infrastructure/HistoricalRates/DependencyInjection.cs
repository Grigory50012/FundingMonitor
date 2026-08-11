using Microsoft.Extensions.DependencyInjection;

namespace FundingMonitor.Infrastructure.HistoricalRates;

public static class DependencyInjection
{
    public static IServiceCollection AddHistoricalRatesInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<RabbitMqHealthState>();
        services.AddSingleton<RabbitMqTopologyInitializer>();
        services.AddSingleton<IRabbitMqMessagePublisher, RabbitMqMessagePublisher>();
        services.AddSingleton<IHistoricalCollectionTaskPublisher,
            DirectRabbitMqHistoricalCollectionTaskPublisher>();
        services.AddSingleton<IHistoricalRetryDelay, HistoricalRetryDelay>();
        services.AddScoped<HistoricalMessageProcessor>();
        services.AddScoped<IHistoricalFundingRateRepository, HistoricalFundingRateRepository>();
        services.AddHostedService<RabbitMqTopologyHostedService>();
        services.AddHostedService<HistoricalCollectionBackgroundService>();
        services.AddHealthChecks()
            .AddCheck<RabbitMqTopologyHealthCheck>("rabbitmq-topology");

        return services;
    }
}
