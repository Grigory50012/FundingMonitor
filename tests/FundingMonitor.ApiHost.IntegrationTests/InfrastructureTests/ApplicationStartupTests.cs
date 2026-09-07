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
    public async Task Start_EmptyDatabase_AppliesAllMigrations()
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FundingMonitorDbContext>();

        var cancellationToken = TestContext.Current.CancellationToken;
        var pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        var applied = await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);

        Assert.Empty(pending);
        Assert.NotEmpty(applied);
    }

    [Fact]
    public void Start_TestHost_DoesNotRegisterApplicationWorkers()
    {
        var hostedServices = Fixture.Factory.Services.GetServices<IHostedService>().ToList();

        Assert.DoesNotContain(hostedServices, service =>
            service is CurrentCollectionBackgroundService
                or HistoricalCollectionBackgroundService
                or RabbitMqTopologyHostedService);
    }
}