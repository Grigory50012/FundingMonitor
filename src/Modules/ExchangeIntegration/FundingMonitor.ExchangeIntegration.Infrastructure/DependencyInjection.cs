using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Objects.Options;
using Bybit.Net;
using Bybit.Net.Clients;
using Bybit.Net.Objects.Options;
using FundingMonitor.ExchangeIntegration.Application;
using FundingMonitor.ExchangeIntegration.Domain;
using FundingMonitor.ExchangeIntegration.Infrastructure.Gateways;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OKX.Net;
using OKX.Net.Clients;
using OKX.Net.Objects.Options;

namespace FundingMonitor.ExchangeIntegration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddExchangeIntegrationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration;

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<SymbolNormalizer>();
        services.AddSingleton(_ => new BinanceRestClient(
            null,
            null,
            Options.Create(new BinanceRestOptions
            {
                Environment = BinanceEnvironment.Live,
                AutoTimestamp = true,
                TimestampRecalculationInterval = TimeSpan.FromHours(1),
                HttpVersion = new Version(2, 0),
                HttpKeepAliveInterval = TimeSpan.FromSeconds(15),
                RateLimiterEnabled = true,
                OutputOriginalData = true,
                CachingEnabled = false
            })));
        services.AddSingleton(_ => new BybitRestClient(
            null,
            null,
            Options.Create(new BybitRestOptions
            {
                Environment = BybitEnvironment.Live,
                AutoTimestamp = true,
                TimestampRecalculationInterval = TimeSpan.FromHours(1),
                HttpVersion = new Version(2, 0),
                HttpKeepAliveInterval = TimeSpan.FromSeconds(15),
                RateLimiterEnabled = true,
                OutputOriginalData = true,
                CachingEnabled = false
            })));
        services.AddSingleton(_ => new OKXRestClient(
            null,
            null,
            Options.Create(new OKXRestOptions
            {
                Environment = OKXEnvironment.Live,
                AutoTimestamp = true,
                TimestampRecalculationInterval = TimeSpan.FromHours(1),
                HttpVersion = new Version(2, 0),
                HttpKeepAliveInterval = TimeSpan.FromSeconds(15),
                RateLimiterEnabled = true,
                OutputOriginalData = true,
                CachingEnabled = false
            })));

        services.AddScoped<IExchangeGateway, BinanceExchangeGateway>();
        services.AddScoped<IExchangeGateway, BybitExchangeGateway>();
        services.AddScoped<IExchangeGateway, OkxExchangeGateway>();

        return services;
    }
}
