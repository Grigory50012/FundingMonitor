using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.Infrastructure.Collections;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL integration tests";
}