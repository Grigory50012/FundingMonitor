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
        services.AddSingleton<IStateRepository, InMemorySymbolStateRepository>();

        // Регистрируем репозитории
        services.AddScoped<ICurrentFundingRateRepository, CurrentFundingRateRepository>();
        services.AddScoped<IHistoricalFundingRateRepository, HistoricalFundingRateRepository>();

        // Регистрируем клиенты бирж с rate limiting
        services.AddScoped<IExchangeFundingRateClient, BinanceFundingRateClient>();
        services.AddScoped<IExchangeFundingRateClient, BybitFundingRateClient>();

        services.AddSingleton<IHistoryTaskQueue, InMemoryHistoryTaskQueue>();
    }
}