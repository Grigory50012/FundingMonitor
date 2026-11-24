using FundingMonitor.ExchangeIntegration.Application;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.ExchangeIntegration.Infrastructure;
using FundingMonitor.ExchangeIntegration.Presentation.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace FundingMonitor.ExchangeIntegration.Presentation;

public static class ExchangeIntegrationModule
{
    public static void AddExchangeIntegrationModule(this IServiceCollection services)
    {
        services.AddExchangeIntegrationInfrastructure();
        services.AddScoped<IExchangeMarketDataModule, ExchangeMarketDataModule>();
        services.AddControllers().AddApplicationPart(typeof(ExchangesController).Assembly);
    }
}
