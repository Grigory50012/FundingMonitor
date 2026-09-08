using FundingMonitor.CurrentRates.Infrastructure.Persistence;
using FundingMonitor.CurrentRates.Infrastructure;
using FundingMonitor.DatabaseMigrator;
using FundingMonitor.HistoricalRates.Infrastructure.Persistence;
using FundingMonitor.HistoricalRates.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.InfrastructureTests;

public sealed class ModuleDatabaseMigratorTests
{
    [Fact]
    public async Task MigrateAsync_FreshDatabase_CreatesOnlyOwnedSchemasAndTables()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var container = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await container.StartAsync(cancellationToken);
        await using (var apiFactory = new FundingMonitorApiFactory(container.GetConnectionString()))
        {
            _ = apiFactory.Services;
            await using var emptyConnection = new NpgsqlConnection(container.GetConnectionString());
            await emptyConnection.OpenAsync(cancellationToken);
            await using var emptyTables = new NpgsqlCommand("""
                SELECT COUNT(*) FROM information_schema.tables
                WHERE table_schema NOT IN ('pg_catalog', 'information_schema');
                """, emptyConnection);
            Assert.Equal(0L, await emptyTables.ExecuteScalarAsync(cancellationToken));
        }
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = container.GetConnectionString()
            }).Build();
        var services = new ServiceCollection();
        services.AddCurrentRatesPersistence(configuration);
        services.AddHistoricalRatesPersistence(configuration);
        services.AddScoped<ModuleDatabaseMigrator>();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        Assert.Empty(provider.GetServices<IHostedService>());
        await using var scope = provider.CreateAsyncScope();
        var migrator = scope.ServiceProvider.GetRequiredService<ModuleDatabaseMigrator>();
        await migrator.MigrateAsync(cancellationToken);
        // A second migration run must preserve independently recorded migrations and owned schema.
        await migrator.MigrateAsync(cancellationToken);
        var currentRates = scope.ServiceProvider.GetRequiredService<CurrentRatesDbContext>();
        var historicalRates = scope.ServiceProvider.GetRequiredService<HistoricalRatesDbContext>();
        Assert.False(currentRates.Database.HasPendingModelChanges());
        Assert.False(historicalRates.Database.HasPendingModelChanges());

        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            SELECT table_schema, table_name
            FROM information_schema.tables
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema');
            """, connection);
        var tables = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                tables.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
        }

        Assert.Equal(new[]
        {
            "current_rates.CurrentFundingRates",
            "current_rates.OutboxMessages",
            "current_rates.__EFMigrationsHistory",
            "historical_rates.HistoricalFundingRates",
            "historical_rates.InboxMessages",
            "historical_rates.__EFMigrationsHistory"
        }.Order(StringComparer.Ordinal), tables.Order(StringComparer.Ordinal));

        await using var schemas = new NpgsqlCommand("""
            SELECT schema_name FROM information_schema.schemata
            WHERE schema_name IN ('arbitrage', 'exchange_integration');
            """, connection);
        await using var schemaReader = await schemas.ExecuteReaderAsync(cancellationToken);
        Assert.False(await schemaReader.ReadAsync(cancellationToken));
    }
}
