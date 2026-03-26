using FundingMonitor.Application.BackgroundServices;
using FundingMonitor.Application.Services;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FundingMonitor.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        // Регистрируем сервисы приложения
        services.AddScoped<ICurrentFundingRateCollector, CurrentFundingRateCollector>();
        services.AddScoped<IExchangeAvailabilityChecker, ExchangeAvailabilityChecker>();
        services.AddScoped<IHistoricalCollectionProducer, HistoricalCollectionProducer>();
        services.AddScoped<IAprStatsService, AprStatsService>();

        services.AddSingleton<ISymbolService, SymbolService>();

        services.AddHostedService<CurrentCollectionBackgroundService>();
        services.AddHostedService<HistoricalCollectionBackgroundService>();
    }
}