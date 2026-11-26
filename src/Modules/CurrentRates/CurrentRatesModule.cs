using FundingMonitor.CurrentRates.CollectCurrentRates;
using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates.GetCurrentRates;
using FundingMonitor.CurrentRates.Persistence;
using FundingMonitor.CurrentRates.PublishPendingEvents;
using FundingMonitor.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FundingMonitor.CurrentRates;

public static class CurrentRatesModule
{
    extension(IServiceCollection services)
    {
        public void AddCurrentRatesModule(IConfiguration configuration)
        {
            services.TryAddSingleton(TimeProvider.System);
            services.AddCurrentRatesPersistence(configuration);
            services.AddFundingMonitorMessaging(configuration);
            services.AddScoped<ICurrentRatesRepository, CurrentRatesRepository>();

            services.AddScoped<ICurrentRatesModule, CurrentRatesReader>();
            services.AddControllers().AddApplicationPart(typeof(FundingRatesController).Assembly);

            services.AddOptions<CurrentDataCollectionOptions>()
                .Bind(configuration.GetSection(CurrentDataCollectionOptions.SectionName));
            services.AddScoped<CurrentFundingRateCollector>();
            services.AddHostedService<CurrentCollectionBackgroundService>();

            services.AddOptions<OutboxOptions>()
                .Bind(configuration.GetSection(OutboxOptions.SectionName));
            services.AddSingleton<OutboxPublisher>();
            services.AddHostedService<OutboxPublisherBackgroundService>();
        }

        public void AddCurrentRatesPersistence(IConfiguration configuration)
        {
            services.AddDbContextFactory<CurrentRatesDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsql =>
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "current_rates")));
        }
    }
}
