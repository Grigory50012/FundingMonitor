using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.InfrastructureTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApplicationStartupTests(PostgreSqlFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Start_PreMigratedDatabase_HasAllModuleMigrations()
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var current = scope.ServiceProvider.GetRequiredService<CurrentRatesDbContext>();
        var historical = scope.ServiceProvider.GetRequiredService<FundingMonitor.HistoricalRates.Infrastructure.Persistence.HistoricalRatesDbContext>();

        var cancellationToken = TestContext.Current.CancellationToken;
        Assert.Empty(await current.Database.GetPendingMigrationsAsync(cancellationToken));
        Assert.Empty(await historical.Database.GetPendingMigrationsAsync(cancellationToken));
        Assert.Equal(current.Database.GetMigrations(),
            await current.Database.GetAppliedMigrationsAsync(cancellationToken));
        Assert.Equal(historical.Database.GetMigrations(),
            await historical.Database.GetAppliedMigrationsAsync(cancellationToken));
    }

    [Fact]
    public void Start_TestHost_DoesNotRegisterApplicationWorkers()
    {
        var hostedServices = Fixture.Factory.Services.GetServices<IHostedService>().ToList();

        Assert.DoesNotContain(hostedServices, service =>
            service.GetType().Namespace?.StartsWith("FundingMonitor.", StringComparison.Ordinal) == true);
    }
}
