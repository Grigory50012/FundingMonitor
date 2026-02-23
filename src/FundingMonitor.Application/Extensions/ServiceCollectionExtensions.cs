using FundingMonitor.Application.Services;
using FundingMonitor.Core.Events;
using FundingMonitor.Core.Interfaces.Events;
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

        services.AddSingleton<ISymbolNormalizer, SymbolNormalizer>();

        services.AddSingleton<HistoricalDataEventConsumer>();

        services.AddSingleton<IEventSubscriber<NewSymbolDetectedEvent>>(sp =>
            sp.GetRequiredService<HistoricalDataEventConsumer>());
        services.AddSingleton<IEventSubscriber<FundingTimeChangedEvent>>(sp =>
            sp.GetRequiredService<HistoricalDataEventConsumer>());
    }
}