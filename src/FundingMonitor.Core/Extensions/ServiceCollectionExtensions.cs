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
        services.Configure<HistoricalCollectionOptions>(
            configuration.GetSection(HistoricalCollectionOptions.SectionName));
        services.Configure<RateLimitOptions>(
            configuration.GetSection(RateLimitOptions.SectionName));
    }
}