using FundingMonitor.Application.BackgroundServices;
using FundingMonitor.Infrastructure.BackgroundServices;
using FundingMonitor.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FundingMonitor.Api.IntegrationTests.Infrastructure;

public sealed class FundingMonitorApiFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var workerTypes = new HashSet<Type>
            {
                typeof(CurrentCollectionBackgroundService),
                typeof(HistoricalCollectionBackgroundService),
                typeof(RabbitMqTopologyHostedService)
            };

            var workerDescriptors = services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(IHostedService) &&
                    descriptor.ImplementationType is not null &&
                    workerTypes.Contains(descriptor.ImplementationType))
                .ToList();

            foreach (var descriptor in workerDescriptors)
                services.Remove(descriptor);

            services.RemoveAll<FundingMonitorDbContext>();
            services.RemoveAll<IDbContextFactory<FundingMonitorDbContext>>();
            services.RemoveAll<DbContextOptions<FundingMonitorDbContext>>();

            services.AddDbContextFactory<FundingMonitorDbContext>(options =>
                options.UseNpgsql(connectionString));
        });
    }
}
