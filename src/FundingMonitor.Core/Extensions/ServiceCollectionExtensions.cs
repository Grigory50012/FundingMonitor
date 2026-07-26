using FundingMonitor.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FundingMonitor.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddCoreServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Регистрация конфигурационных секций
        services.Configure<CurrentDataCollectionOptions>(
            configuration.GetSection(CurrentDataCollectionOptions.SectionName));
        services.AddOptions<HistoricalDataCollectionOptions>()
            .Bind(configuration.GetSection(HistoricalDataCollectionOptions.SectionName))
            .Validate(
                options => options.DefaultConcurrencyPerExchange >= 0,
                "DefaultConcurrencyPerExchange must be non-negative")
            .Validate(
                options => options.ExchangeConcurrency.Values.All(value => value >= 0),
                "ExchangeConcurrency values must be non-negative")
            .Validate(
                options => options.ApiPageSize > 0,
                "ApiPageSize must be positive")
            .Validate(
                options => options.MaxHistoryMonths > 0,
                "MaxHistoryMonths must be positive")
            .Validate(
                options => options.MaxRetries >= 0,
                "MaxRetries must be non-negative")
            .Validate(
                options => options.RetryDelaySeconds > 0,
                "RetryDelaySeconds must be positive")
            .ValidateOnStart();
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "Host is required")
            .Validate(options => options.Port is > 0 and <= 65535, "Port is invalid")
            .Validate(
                options => options.PublisherConfirmTimeoutSeconds > 0,
                "PublisherConfirmTimeoutSeconds must be positive")
            .ValidateOnStart();
        services.Configure<FundingArbitrageOptions>(
            configuration.GetSection(FundingArbitrageOptions.SectionName));
    }
}
