using Xunit;

namespace FundingMonitor.Api.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase(PostgreSqlFixture fixture) : IAsyncLifetime
{
    protected PostgreSqlFixture Fixture { get; } = fixture;

    public async ValueTask InitializeAsync()
    {
        await Fixture.ResetAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
