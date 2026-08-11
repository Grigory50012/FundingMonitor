using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace FundingMonitor.Api.IntegrationTests.Infrastructure;

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

        _ = Factory.Services;

        _resetter = await DatabaseResetter.CreateAsync(_container.GetConnectionString());
    }

    public async ValueTask DisposeAsync()
    {
        if (Factory is not null)
            await Factory.DisposeAsync();

        await _container.DisposeAsync();
    }

    public Task ResetAsync() => _resetter.ResetAsync();

    public async Task SeedAsync(params CurrentFundingRateDb[] rates)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FundingMonitorDbContext>();
        dbContext.CurrentFundingRate.AddRange(rates);
        await dbContext.SaveChangesAsync();
    }
}
