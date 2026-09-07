using FundingMonitor.CurrentRates.Application.Collection;
using FundingMonitor.CurrentRates.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FundingMonitor.CurrentRates.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCurrentRatesApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<CurrentDataCollectionOptions>()
            .Bind(configuration.GetSection(CurrentDataCollectionOptions.SectionName));
        services.AddScoped<ICurrentRatesModule, CurrentRatesService>();
        services.AddScoped<IFundingRateChangeDetector, FundingRateChangeDetector>();
        services.AddScoped<ICurrentFundingRateCollector, CurrentFundingRateCollector>();
        services.AddHostedService<CurrentCollectionBackgroundService>();
        return services;
    }
}
