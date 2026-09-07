using FundingMonitor.ExchangeIntegration.Application;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.ExchangeIntegration.Infrastructure;
using FundingMonitor.ExchangeIntegration.Presentation.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FundingMonitor.ExchangeIntegration.Presentation;

public static class ExchangeIntegrationModule
{
    public static IServiceCollection AddExchangeIntegrationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddExchangeIntegrationInfrastructure(configuration);
        services.AddScoped<IExchangeMarketDataModule, ExchangeMarketDataModule>();
        services.AddControllers().AddApplicationPart(typeof(ExchangesController).Assembly);
        return services;
    }

    public static IEndpointRouteBuilder MapExchangeIntegrationEndpoints(
        this IEndpointRouteBuilder endpoints) => endpoints;
}
