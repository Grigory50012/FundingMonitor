using FundingMonitor.CurrentRates.Application;
using FundingMonitor.CurrentRates.Infrastructure;
using FundingMonitor.CurrentRates.Presentation.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FundingMonitor.CurrentRates.Presentation;

public static class CurrentRatesModule
{
    public static IServiceCollection AddCurrentRatesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCurrentRatesApplication(configuration);
        services.AddCurrentRatesInfrastructure(configuration);
        services.AddControllers().AddApplicationPart(typeof(FundingRatesController).Assembly);
        return services;
    }

    public static IEndpointRouteBuilder MapCurrentRatesEndpoints(
        this IEndpointRouteBuilder endpoints) => endpoints;
}
