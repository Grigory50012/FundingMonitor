using FundingMonitor.HistoricalRates.Application;
using FundingMonitor.HistoricalRates.Application.Persistence;
using FundingMonitor.HistoricalRates.Domain;
using FundingMonitor.HistoricalRates.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace FundingMonitor.HistoricalRates.IntegrationTests.Persistence;

[Collection("HistoricalRatesPostgreSql")]
public sealed class HistoricalPersistenceTests(HistoricalRatesPostgreSqlFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepeatedFundingPoint_UpsertsOneRow(bool sameEventId)
    {
        await fixture.ResetAsync();
        var market = new CountingExchangeMarketDataModule([TestHistory.BtcPoint]);
        await using var scope = await fixture.CreateScopeAsync(market);
        var handler = scope.ServiceProvider.GetRequiredService<IHistoricalEventHandler>();
        await handler.HandleAsync(TestEvents.BtcChanged, TestContext.Current.CancellationToken);
        await handler.HandleAsync(sameEventId ? TestEvents.BtcChanged : TestEvents.BtcChanged with { EventId = Guid.NewGuid() },
            TestContext.Current.CancellationToken);

        Assert.Equal(2, market.HistoricalRequestCount);
        await using var verification = await fixture.CreateDbContextAsync();
        var point = Assert.Single(await verification.HistoricalFundingRates.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(TestHistory.BtcPoint.FundingRate, point.FundingRate);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentDeliveries_UpsertOneRow(bool sameEventId)
    {
        await fixture.ResetAsync();
        await using var first = await fixture.CreateScopeAsync(new CountingExchangeMarketDataModule([TestHistory.BtcPoint]));
        await using var second = await fixture.CreateScopeAsync(new CountingExchangeMarketDataModule([TestHistory.BtcPoint]));
        await Task.WhenAll(
            first.ServiceProvider.GetRequiredService<IHistoricalEventHandler>().HandleAsync(TestEvents.BtcChanged, TestContext.Current.CancellationToken),
            second.ServiceProvider.GetRequiredService<IHistoricalEventHandler>().HandleAsync(
                sameEventId ? TestEvents.BtcChanged : TestEvents.BtcChanged with { EventId = Guid.NewGuid() }, TestContext.Current.CancellationToken));

        await using var verification = await fixture.CreateDbContextAsync();
        Assert.Single(await verification.HistoricalFundingRates.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InvalidPointInBatch_RollsBackOtherPointUpdates()
    {
        await fixture.ResetAsync();
        await using var scope = await fixture.CreateScopeAsync(new CountingExchangeMarketDataModule([TestHistory.BtcPoint]));
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IHistoricalRatesUnitOfWork>();
        var point = TestHistory.BtcPoint;
        var original = new HistoricalFundingRate(point.Exchange, point.Symbol, point.FundingRate, point.FundingTime, point.CollectedOnUtc);
        await unitOfWork.StoreHistoryAsync([original], TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => unitOfWork.StoreHistoryAsync(
            [original with { FundingRate = 0.123m }, original with { FundingTime = point.FundingTime.AddHours(8), FundingRate = decimal.MaxValue }],
            TestContext.Current.CancellationToken));

        Assert.Equal(PostgresErrorCodes.NumericValueOutOfRange, exception.SqlState);
        await using var verification = await fixture.CreateDbContextAsync();
        var stored = Assert.Single(await verification.HistoricalFundingRates.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(point.FundingRate, stored.FundingRate);
    }

    [Fact]
    public async Task ChangedFundingPoint_UpdatesExistingRow()
    {
        await fixture.ResetAsync();
        await using var scope = await fixture.CreateScopeAsync(new CountingExchangeMarketDataModule([]));
        var store = scope.ServiceProvider.GetRequiredService<IHistoricalRatesUnitOfWork>();
        var point = TestHistory.BtcPoint;
        var original = new HistoricalFundingRate(point.Exchange, point.Symbol, point.FundingRate, point.FundingTime, point.CollectedOnUtc);
        await store.StoreHistoryAsync([original], TestContext.Current.CancellationToken);
        await store.StoreHistoryAsync([original with { FundingRate = 0.123m, CollectedOnUtc = point.CollectedOnUtc.AddMinutes(1) }], TestContext.Current.CancellationToken);
        await using var verification = await fixture.CreateDbContextAsync();
        var stored = Assert.Single(await verification.HistoricalFundingRates.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0.123m, stored.FundingRate);
        Assert.Equal(point.CollectedOnUtc.AddMinutes(1), stored.CollectedOnUtc);
    }
}
