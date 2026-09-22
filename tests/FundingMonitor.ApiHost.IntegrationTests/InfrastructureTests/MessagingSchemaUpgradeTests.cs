using FundingMonitor.CurrentRates.Infrastructure.Persistence;
using FundingMonitor.HistoricalRates.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.InfrastructureTests;

public sealed class MessagingSchemaUpgradeTests
{
    [Fact]
    public async Task Upgrade_PreservesPendingEventAndHistory_RemovesOnlyRetiredSchema()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var container = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await container.StartAsync(ct);
        await using var current = new CurrentRatesDbContext(new DbContextOptionsBuilder<CurrentRatesDbContext>()
            .UseNpgsql(container.GetConnectionString(), options => options.MigrationsHistoryTable("__EFMigrationsHistory", "current_rates")).Options);
        await using var history = new HistoricalRatesDbContext(new DbContextOptionsBuilder<HistoricalRatesDbContext>()
            .UseNpgsql(container.GetConnectionString(), options => options.MigrationsHistoryTable("__EFMigrationsHistory", "historical_rates")).Options);
        await current.GetService<IMigrator>().MigrateAsync("20260906000100_InitialCurrentRates", ct);
        await history.GetService<IMigrator>().MigrateAsync("20260906000200_InitialHistoricalRates", ct);
        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync(ct);
        const string payload = """{"eventId":"20000000-0000-0000-0000-000000000001","occurredOnUtc":"2026-09-12T00:00:00Z","exchange":"Binance","symbol":"BTC-USDT","nextFundingTime":null}""";
        await using (var seed = new NpgsqlCommand("""
            INSERT INTO current_rates."OutboxMessages"
                ("Id", "Type", "Payload", "OccurredOnUtc", "Attempts", "Error", "LockOwner", "LockedUntilUtc")
            VALUES ('20000000-0000-0000-0000-000000000001', 'FundingMonitor.CurrentRates.Contracts.FundingRateChangedIntegrationEvent',
                @payload::jsonb, '2026-09-12T00:00:00Z', 2, 'offline', 'stopped-instance', '2099-01-01T00:00:00Z');
            INSERT INTO historical_rates."HistoricalFundingRates"
                ("Exchange", "NormalizedSymbol", "FundingTime", "FundingRate", "CollectedOnUtc")
            VALUES ('Binance', 'BTC-USDT', '2026-09-12T00:00:00Z', 0.0003, '2026-09-12T01:00:00Z');
            INSERT INTO historical_rates."InboxMessages" ("EventId", "Consumer", "ProcessedOnUtc")
            VALUES ('20000000-0000-0000-0000-000000000001', 'HistoricalRates.FundingRateChanged.v1', '2026-09-12T01:00:00Z');
            """, connection))
        {
            seed.Parameters.AddWithValue("payload", payload);
            await seed.ExecuteNonQueryAsync(ct);
        }

        await current.Database.MigrateAsync(ct);
        await history.Database.MigrateAsync(ct);

        var message = Assert.Single(await current.OutboxMessages.AsNoTracking().ToListAsync(ct));
        Assert.Null(message.ProcessedOnUtc);
        Assert.Equal(2, message.Attempts);
        Assert.Equal("offline", message.Error);
        await using var columns = new NpgsqlCommand("""
            SELECT "Exchange", "Symbol", "NextFundingTime"
            FROM current_rates."OutboxMessages"
            WHERE "Id" = '20000000-0000-0000-0000-000000000001';
            """, connection);
        await using (var migrated = await columns.ExecuteReaderAsync(ct))
        {
            Assert.True(await migrated.ReadAsync(ct));
            Assert.Equal("Binance", migrated.GetString(0));
            Assert.Equal("BTC-USDT", migrated.GetString(1));
            Assert.True(await migrated.IsDBNullAsync(2, ct));
        }
        Assert.Equal(0.0003m, Assert.Single(await history.HistoricalFundingRates.AsNoTracking().ToListAsync(ct)).FundingRate);
        Assert.False(current.Database.HasPendingModelChanges());
        Assert.False(history.Database.HasPendingModelChanges());
        await using var retiredSchema = new NpgsqlCommand("""
            SELECT
                (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = 'current_rates'
                    AND table_name = 'OutboxMessages' AND column_name IN ('Type', 'LockOwner', 'LockedUntilUtc'))
                + (SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'historical_rates' AND table_name = 'InboxMessages');
            """, connection);
        Assert.Equal(0L, await retiredSchema.ExecuteScalarAsync(ct));

        await current.GetService<IMigrator>().MigrateAsync("20260912145113_SimplifyOutbox", ct);
        await using var restored = new NpgsqlCommand("""
            SELECT "Payload" FROM current_rates."OutboxMessages"
            WHERE "Id" = '20000000-0000-0000-0000-000000000001';
            """, connection);
        using var restoredJson = System.Text.Json.JsonDocument.Parse(
            Assert.IsType<string>(await restored.ExecuteScalarAsync(ct)));
        Assert.Equal("Binance", restoredJson.RootElement.GetProperty("exchange").GetString());
        Assert.Equal("BTC-USDT", restoredJson.RootElement.GetProperty("symbol").GetString());
        Assert.Equal("20000000-0000-0000-0000-000000000001",
            restoredJson.RootElement.GetProperty("eventId").GetString());
    }
}
