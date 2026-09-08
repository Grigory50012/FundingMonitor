using FundingMonitor.HistoricalRates.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FundingMonitor.HistoricalRates.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddHistoricalRatesApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<HistoricalDataCollectionOptions>()
            .Bind(configuration.GetSection(HistoricalDataCollectionOptions.SectionName))
            .Validate(options => options.DefaultConcurrencyPerExchange >= 0)
            .Validate(options => options.ExchangeConcurrency.Values.All(value => value >= 0))
            .Validate(options => options.ApiPageSize > 0)
            .Validate(options => options.MaxHistoryMonths > 0)
            .Validate(options => options.MaxRetries >= 0)
            .Validate(options => options.RetryDelaySeconds > 0)
            .ValidateOnStart();
        services.AddOptions<AprStatsOptions>()
            .Bind(configuration.GetSection(AprStatsOptions.SectionName));
        services.AddMemoryCache(options => options.SizeLimit = 1000);
        services.AddScoped<IHistoricalRatesModule, HistoricalRatesService>();
        services.AddScoped<IHistoricalEventHandler, HistoricalEventHandler>();
        services.AddSingleton<IHistoricalCollectionFailureClassifier, HistoricalCollectionFailureClassifier>();
        return services;
    }
}
