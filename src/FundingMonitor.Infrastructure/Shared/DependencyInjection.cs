using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Objects.Options;
using Bybit.Net;
using Bybit.Net.Clients;
using Bybit.Net.Objects.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OKX.Net;
using OKX.Net.Clients;
using OKX.Net.Objects.Options;

namespace FundingMonitor.Infrastructure.Shared;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<BinanceRestClient>(_ => new BinanceRestClient(null, null,
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
        services.AddSingleton<BybitRestClient>(_ => new BybitRestClient(null, null,
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
        services.AddSingleton<OKXRestClient>(_ => new OKXRestClient(null, null,
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

        services.AddDbContextFactory<FundingMonitorDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>()
                .GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IExchangeFundingRateClient, BinanceFundingRateClient>();
        services.AddScoped<IExchangeFundingRateClient, BybitFundingRateClient>();
        services.AddScoped<IExchangeFundingRateClient, OkxFundingRateClient>();

        return services;
    }
}
