using FundingMonitor.Application.BackgroundServices;
using FundingMonitor.Application.Services;
using FundingMonitor.Application.Services.Arbitrage;
using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FundingMonitor.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICurrentFundingRateCollector, CurrentFundingRateCollector>();
        services.AddScoped<IHistoricalFundingRateCollector, HistoricalFundingRateCollector>();
        services.AddScoped<IExchangeAvailabilityChecker, ExchangeAvailabilityChecker>();
        services.AddScoped<IHistoricalCollectionProducer, HistoricalCollectionProducer>();
        services.AddScoped<IAprStatsService, AprStatsService>();
        services.AddScoped<IFundingRateChangeDetector, FundingRateChangeDetector>();

        services.AddSingleton<ISymbolService, SymbolService>();

        services.Configure<AprStatsOptions>(configuration.GetSection(AprStatsOptions.SectionName));

        // Arbitrage
        services.AddSingleton<IFundingArbitrageService, FundingArbitrageService>();
        services.AddScoped<IFundingArbitrageDetector, FundingArbitrageDetector>();

        services.AddHostedService<CurrentCollectionBackgroundService>();
        services.AddHostedService<HistoricalCollectionBackgroundService>();
    }
}