using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Objects.Options;
using Bybit.Net;
using Bybit.Net.Clients;
using Bybit.Net.Objects.Options;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Core.Interfaces.Services;
using FundingMonitor.Infrastructure.BackgroundServices;
using FundingMonitor.Infrastructure.Data;
using FundingMonitor.Infrastructure.Data.Repositories;
using FundingMonitor.Infrastructure.ExchangeClients;
using FundingMonitor.Infrastructure.HealthChecks;
using FundingMonitor.Infrastructure.Messaging.RabbitMq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OKX.Net;
using OKX.Net.Clients;
using OKX.Net.Objects.Options;

namespace FundingMonitor.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<RabbitMqHealthState>();
        services.AddSingleton<RabbitMqTopologyInitializer>();
        services.AddSingleton<IRabbitMqMessagePublisher, RabbitMqMessagePublisher>();
        services.AddSingleton<IHistoricalCollectionTaskPublisher,
            DirectRabbitMqHistoricalCollectionTaskPublisher>();
        services.AddSingleton<IHistoricalRetryDelay, HistoricalRetryDelay>();
        services.AddScoped<HistoricalMessageProcessor>();
        services.AddHostedService<RabbitMqTopologyHostedService>();
        services.AddHostedService<HistoricalCollectionBackgroundService>();
        services.AddHealthChecks()
            .AddCheck<RabbitMqTopologyHealthCheck>("rabbitmq-topology");

        // Регистрируем REST клиенты бирж (Singleton)
        services.AddSingleton<BinanceRestClient>(_ =>
        {
            var options = Options.Create(new BinanceRestOptions
            {
                Environment = BinanceEnvironment.Live,
                AutoTimestamp = true,
                TimestampRecalculationInterval = TimeSpan.FromHours(1),
                HttpVersion = new Version(2, 0),
                HttpKeepAliveInterval = TimeSpan.FromSeconds(15),
                RateLimiterEnabled = true,
                OutputOriginalData = true,
                CachingEnabled = false
            });
            return new BinanceRestClient(null, null, options);
        });

        services.AddSingleton<BybitRestClient>(_ =>
        {
            var options = Options.Create(new BybitRestOptions
            {
                Environment = BybitEnvironment.Live,
                AutoTimestamp = true,
                TimestampRecalculationInterval = TimeSpan.FromHours(1),
                HttpVersion = new Version(2, 0),
                HttpKeepAliveInterval = TimeSpan.FromSeconds(15),
                RateLimiterEnabled = true,
                OutputOriginalData = true,
                CachingEnabled = false
            });
            return new BybitRestClient(null, null, options);
        });

        services.AddSingleton<OKXRestClient>(_ =>
        {
            var options = Options.Create(new OKXRestOptions
            {
                Environment = OKXEnvironment.Live,
                AutoTimestamp = true,
                TimestampRecalculationInterval = TimeSpan.FromHours(1),
                HttpVersion = new Version(2, 0),
                HttpKeepAliveInterval = TimeSpan.FromSeconds(15),
                RateLimiterEnabled = true,
                OutputOriginalData = true,
                CachingEnabled = false
            });
            return new OKXRestClient(null, null, options);
        });

        // Добавляем фабрику DbContext
        services.AddDbContextFactory<FundingMonitorDbContext>((sp, options) =>
        {
            var connectionStrings = sp.GetRequiredService<IConfiguration>()
                .GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionStrings);
        });

        // Регистрируем репозитории
        services.AddScoped<ICurrentFundingRateRepository, CurrentFundingRateRepository>();
        services.AddScoped<IHistoricalFundingRateRepository, HistoricalFundingRateRepository>();

        // Регистрируем клиенты бирж с DI
        services.AddScoped<IExchangeFundingRateClient, BinanceFundingRateClient>();
        services.AddScoped<IExchangeFundingRateClient, BybitFundingRateClient>();
        services.AddScoped<IExchangeFundingRateClient, OkxFundingRateClient>();
    }
}
