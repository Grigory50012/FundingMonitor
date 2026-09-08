using FundingMonitor.Arbitrage.Application;
using FundingMonitor.Arbitrage.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FundingMonitor.Arbitrage.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddArbitrageInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<FundingArbitrageOptions>()
            .Bind(configuration.GetSection(FundingArbitrageOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<FundingArbitrageService>();
        services.AddScoped<FundingArbitrageDetector>();
        services.AddScoped<ArbitrageModuleService>();
        services.AddScoped<IArbitrageModule>(provider =>
            provider.GetRequiredService<ArbitrageModuleService>());
        return services;
    }
}
