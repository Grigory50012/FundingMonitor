using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase(PostgreSqlFixture fixture) : IAsyncLifetime
{
    protected PostgreSqlFixture Fixture { get; } = fixture;

    public async ValueTask InitializeAsync()
    {
        await Fixture.ResetAsync();
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}