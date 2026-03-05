using FundingMonitor.Application.Services;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FundingMonitor.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        // Регистрируем сервисы приложения
        services.AddScoped<ICurrentDataCollector, CurrentDataCollector>();
        services.AddScoped<IExchangeHealthChecker, ExchangeHealthChecker>();
        services.AddScoped<IHistoricalDataCollector, HistoricalDataCollector>();

        services.AddSingleton<ISymbolNormalizer, SymbolNormalizer>();
    }
}