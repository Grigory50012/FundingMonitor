using FundingMonitor.HistoricalRates.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FundingMonitor.ApiHost.IntegrationTests.Infrastructure;

public sealed class FundingMonitorApiFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            foreach (var descriptor in services.Where(descriptor =>
                         descriptor.ServiceType == typeof(IHostedService) &&
                         descriptor.ImplementationType?.Namespace?.StartsWith(
                             "FundingMonitor.", StringComparison.Ordinal) == true).ToArray())
                services.Remove(descriptor);

            ReplaceContext<CurrentRatesDbContext>(services, "current_rates");
            ReplaceContext<HistoricalRatesDbContext>(services, "historical_rates");
        });
    }

    private void ReplaceContext<TContext>(IServiceCollection services, string schema)
        where TContext : DbContext
    {
        services.RemoveAll<TContext>();
        services.RemoveAll<IDbContextFactory<TContext>>();
        services.RemoveAll<DbContextOptions<TContext>>();
        services.RemoveAll<IDbContextOptionsConfiguration<TContext>>();
        services.AddDbContextFactory<TContext>(options => options.UseNpgsql(
            connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema)));
    }
}
