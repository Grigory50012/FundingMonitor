using FundingMonitor.CurrentRates.Application.Collection;
using FundingMonitor.CurrentRates.Application.Persistence;
using FundingMonitor.CurrentRates.Infrastructure.Outbox;
using FundingMonitor.CurrentRates.Infrastructure.Persistence;
using FundingMonitor.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FundingMonitor.CurrentRates.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCurrentRatesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<CurrentDataCollectionOptions>()
            .Bind(configuration.GetSection(CurrentDataCollectionOptions.SectionName));
        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName));
        services.AddCurrentRatesPersistence(configuration);
        services.AddFundingMonitorMessaging(configuration);
        services.AddScoped<CurrentRatesRepository>();
        services.AddScoped<ICurrentRatesRepository>(provider =>
            provider.GetRequiredService<CurrentRatesRepository>());
        services.AddScoped<ICurrentRatesUnitOfWork>(provider =>
            provider.GetRequiredService<CurrentRatesRepository>());
        services.AddSingleton<OutboxMetrics>();
        services.AddSingleton<OutboxPublisher>();
        services.AddHostedService<OutboxPublisherBackgroundService>();
        return services;
    }

    public static IServiceCollection AddCurrentRatesPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextFactory<CurrentRatesDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "current_rates")));
        return services;
    }
}
