using Microsoft.Extensions.DependencyInjection;

namespace FundingMonitor.Infrastructure.CurrentRates;

public static class DependencyInjection
{
    public static IServiceCollection AddCurrentRatesInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ICurrentFundingRateRepository, CurrentFundingRateRepository>();
        return services;
    }
}
