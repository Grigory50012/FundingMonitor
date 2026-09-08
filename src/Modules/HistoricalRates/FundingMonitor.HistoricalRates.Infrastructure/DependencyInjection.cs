using FundingMonitor.HistoricalRates.Application.Persistence;
using FundingMonitor.HistoricalRates.Infrastructure.Messaging;
using FundingMonitor.HistoricalRates.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FundingMonitor.HistoricalRates.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHistoricalRatesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddHistoricalRatesPersistence(configuration);
        services.AddScoped<HistoricalRatesRepository>();
        services.AddScoped<IHistoricalRatesRepository>(provider =>
            provider.GetRequiredService<HistoricalRatesRepository>());
        services.AddScoped<IHistoricalRatesUnitOfWork>(provider =>
            provider.GetRequiredService<HistoricalRatesRepository>());
        services.AddSingleton<RabbitMqTopologyHealthState>();
        services.AddSingleton<RabbitMqTopologyInitializer>();
        services.AddSingleton<IHistoricalRetryDelay, HistoricalRetryDelay>();
        services.AddSingleton<HistoricalTransportMetrics>();
        services.AddScoped<HistoricalMessageProcessor>();
        services.AddHostedService<RabbitMqTopologyHostedService>();
        services.AddHostedService<HistoricalCollectionBackgroundService>();
        services.AddHealthChecks().AddCheck<RabbitMqTopologyHealthCheck>("rabbitmq-topology");
        return services;
    }

    public static IServiceCollection AddHistoricalRatesPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextFactory<HistoricalRatesDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "historical_rates")));
        return services;
    }
}
