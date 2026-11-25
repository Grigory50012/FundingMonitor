using FundingMonitor.Arbitrage.Calculation;
using FundingMonitor.Arbitrage.GetOpportunities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FundingMonitor.Arbitrage;

public static class ArbitrageModule
{
    public static IServiceCollection AddArbitrageModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<FundingArbitrageOptions>()
            .Bind(configuration.GetSection(FundingArbitrageOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<FundingArbitrageDetector>();
        services.AddScoped(provider =>
            new GetOpportunitiesHandler(provider.GetRequiredService<FundingArbitrageDetector>()));
        services.AddControllers().AddApplicationPart(typeof(ArbitrageController).Assembly);
        return services;
    }
}
