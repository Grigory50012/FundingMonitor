using FundingMonitor.HistoricalRates.Application;
using FundingMonitor.HistoricalRates.Infrastructure;
using FundingMonitor.HistoricalRates.Presentation.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FundingMonitor.HistoricalRates.Presentation;

public static class HistoricalRatesModule
{
    public static IServiceCollection AddHistoricalRatesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHistoricalRatesApplication(configuration);
        services.AddHistoricalRatesInfrastructure(configuration);
        services.AddControllers().AddApplicationPart(typeof(HistoryController).Assembly);
        return services;
    }

    public static IEndpointRouteBuilder MapHistoricalRatesEndpoints(
        this IEndpointRouteBuilder endpoints) => endpoints;
}
