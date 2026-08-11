using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FundingMonitor.Application.CurrentRates;

public static class DependencyInjection
{
    public static IServiceCollection AddCurrentRatesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CurrentDataCollectionOptions>(
            configuration.GetSection(CurrentDataCollectionOptions.SectionName));
        services.Configure<FundingArbitrageOptions>(
            configuration.GetSection(FundingArbitrageOptions.SectionName));

        services.AddScoped<ICurrentFundingRateCollector, CurrentFundingRateCollector>();
        services.AddScoped<IExchangeAvailabilityChecker, ExchangeAvailabilityChecker>();
        services.AddScoped<IFundingRateChangeDetector, FundingRateChangeDetector>();
        services.AddSingleton<ISymbolService, SymbolService>();
        services.AddSingleton<IFundingArbitrageService, FundingArbitrageService>();
        services.AddScoped<IFundingArbitrageDetector, FundingArbitrageDetector>();
        services.AddHostedService<CurrentCollectionBackgroundService>();

        return services;
    }
}
