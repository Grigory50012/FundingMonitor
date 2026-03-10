using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Queues;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Core.Interfaces.State;
using FundingMonitor.Infrastructure.Data;
using FundingMonitor.Infrastructure.Data.Repositories;
using FundingMonitor.Infrastructure.ExchangeClients;
using FundingMonitor.Infrastructure.Queues;
using FundingMonitor.Infrastructure.State;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructureServices(this IServiceCollection services)
    {
        // Добавляем фабрику DbContext
        services.AddDbContextFactory<FundingMonitorDbContext>((sp, options) =>
        {
            var connectionStrings = sp.GetRequiredService<IConfiguration>()
                .GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionStrings);
        });

        // State
        services.AddSingleton<IStateManager, InMemoryStateManager>();

        // Регистрируем репозитории
        services.AddScoped<ICurrentFundingRateRepository, CurrentFundingRateRepository>();
        services.AddScoped<IHistoricalFundingRateRepository, HistoricalFundingRateRepository>();

        // Регистрируем оригинальные клиенты бирж
        services.AddScoped<BinanceApiClient>();
        services.AddScoped<BybitApiClient>();

        // Регистрируем декорированные клиенты с rate limiting
        services.AddScoped<IExchangeApiClient>(sp =>
        {
            var binanceClient = sp.GetRequiredService<BinanceApiClient>();
            var options = sp.GetRequiredService<IOptions<RateLimitOptions>>();
            var logger = sp.GetRequiredService<ILogger<RateLimitedApiClient>>();

            return new RateLimitedApiClient(binanceClient, options, logger);
        });

        services.AddScoped<IExchangeApiClient>(sp =>
        {
            var bybitClient = sp.GetRequiredService<BybitApiClient>();
            var options = sp.GetRequiredService<IOptions<RateLimitOptions>>();
            var logger = sp.GetRequiredService<ILogger<RateLimitedApiClient>>();

            return new RateLimitedApiClient(bybitClient, options, logger);
        });

        services.AddSingleton<IHistoricalCollectionQueue, InMemoryHistoricalCollectionQueue>();
    }
}