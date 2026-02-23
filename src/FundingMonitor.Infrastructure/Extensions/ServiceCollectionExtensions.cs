using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Events;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Core.Interfaces.State;
using FundingMonitor.Infrastructure.Data;
using FundingMonitor.Infrastructure.Data.Repositories;
using FundingMonitor.Infrastructure.Events;
using FundingMonitor.Infrastructure.ExchangeClients;
using FundingMonitor.Infrastructure.State;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FundingMonitor.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Добавляем фабрику DbContext
        services.AddDbContextFactory<AppDbContext>((sp, options) =>
        {
            var connectionStrings = sp.GetRequiredService<IConfiguration>()
                .GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionStrings);
        });

        // State
        services.AddSingleton<IStateManager, InMemoryStateManager>();

        // RabbitMQ
        services.AddSingleton<IConnectionFactory>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
            return new ConnectionFactory
            {
                HostName = options.Host,
                UserName = options.Username,
                Password = options.Password,
                Port = options.Port,
                ConsumerDispatchConcurrency = 1
            };
        });

        services.AddSingleton<IEventPublisher>(sp =>
        {
            var factory = sp.GetRequiredService<IConnectionFactory>();
            var logger = sp.GetRequiredService<ILogger<RabbitMqEventPublisher>>();

            try
            {
                return RabbitMqEventPublisher.CreateAsync(factory, logger)
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Не удалось создать RabbitMQ поставщика");
                throw;
            }
        });

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
    }
}