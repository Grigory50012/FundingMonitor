using Microsoft.Extensions.DependencyInjection;
using FundingMonitor.DatabaseMigrator;
using FundingMonitor.HistoricalRates.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.Infrastructure;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("funding_monitor_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private DatabaseResetter _resetter = null!;

    public FundingMonitorApiFactory Factory { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        Factory = new FundingMonitorApiFactory(_container.GetConnectionString());

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            await new ModuleDatabaseMigrator(
                scope.ServiceProvider.GetRequiredService<CurrentRatesDbContext>(),
                scope.ServiceProvider.GetRequiredService<HistoricalRatesDbContext>())
                .MigrateAsync(TestContext.Current.CancellationToken);
        }

        _resetter = await DatabaseResetter.CreateAsync(_container.GetConnectionString());
    }

    public async ValueTask DisposeAsync()
    {
        if (Factory is not null)
            await Factory.DisposeAsync();

        await _container.DisposeAsync();
    }

    public Task ResetAsync()
    {
        return _resetter.ResetAsync();
    }

    internal async Task SeedAsync(params CurrentFundingRateDb[] rates)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CurrentRatesDbContext>();
        dbContext.CurrentFundingRates.AddRange(rates);
        await dbContext.SaveChangesAsync();
    }
}
