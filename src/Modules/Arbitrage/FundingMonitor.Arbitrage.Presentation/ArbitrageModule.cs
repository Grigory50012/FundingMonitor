using FundingMonitor.Arbitrage.Infrastructure;
using FundingMonitor.Arbitrage.Presentation.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FundingMonitor.Arbitrage.Presentation;

public static class ArbitrageModule
{
    public static IServiceCollection AddArbitrageModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddArbitrageInfrastructure(configuration);
        services.AddControllers().AddApplicationPart(typeof(ArbitrageController).Assembly);
        return services;
    }

    public static IEndpointRouteBuilder MapArbitrageEndpoints(
        this IEndpointRouteBuilder endpoints) => endpoints;
}
