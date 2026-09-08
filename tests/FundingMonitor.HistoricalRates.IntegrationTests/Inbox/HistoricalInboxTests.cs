using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.HistoricalRates.Application;
using FundingMonitor.HistoricalRates.IntegrationTests.Infrastructure;
using FundingMonitor.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FundingMonitor.HistoricalRates.IntegrationTests.Inbox;

[Collection("HistoricalRatesPostgreSql")]
public sealed class HistoricalInboxTests(HistoricalRatesPostgreSqlFixture fixture)
{
    [Fact]
    public async Task HandleAsync_SameEventTwice_FetchesAndStoresOnce()
    {
        await fixture.ResetAsync();
        var exchangeModule = new CountingExchangeMarketDataModule([TestHistory.BtcPoint]);
        await using var scope = await fixture.CreateScopeAsync(exchangeModule);
        var handler = scope.ServiceProvider.GetRequiredService<IHistoricalEventHandler>();

        await handler.HandleAsync(TestEvents.BtcChanged, TestContext.Current.CancellationToken);
        await handler.HandleAsync(TestEvents.BtcChanged, TestContext.Current.CancellationToken);

        Assert.Equal(1, exchangeModule.HistoricalRequestCount);
        await using var verification = await fixture.CreateDbContextAsync();
        Assert.Single(await verification.HistoricalFundingRates.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
        Assert.Single(await verification.InboxMessages.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_NewEvent_CommitsHistoryAndInboxTogether()
    {
        await fixture.ResetAsync();
        await using var scope = await fixture.CreateScopeAsync(
            new CountingExchangeMarketDataModule([TestHistory.BtcPoint]));

        await scope.ServiceProvider.GetRequiredService<IHistoricalEventHandler>()
            .HandleAsync(TestEvents.BtcChanged, TestContext.Current.CancellationToken);

        await using var verification = await fixture.CreateDbContextAsync();
        Assert.Single(await verification.HistoricalFundingRates.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
        Assert.Single(await verification.InboxMessages.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_DifferentEventIdForSamePair_FetchesTwice()
    {
        await fixture.ResetAsync();
        var exchangeModule = new CountingExchangeMarketDataModule([TestHistory.BtcPoint]);
        await using var scope = await fixture.CreateScopeAsync(exchangeModule);
        var handler = scope.ServiceProvider.GetRequiredService<IHistoricalEventHandler>();

        await handler.HandleAsync(TestEvents.BtcChanged, TestContext.Current.CancellationToken);
        await handler.HandleAsync(
            TestEvents.BtcChanged with { EventId = Guid.Parse("30000000-0000-0000-0000-000000000002") },
            TestContext.Current.CancellationToken);

        Assert.Equal(2, exchangeModule.HistoricalRequestCount);
        await using var verification = await fixture.CreateDbContextAsync();
        Assert.Equal(2, await verification.InboxMessages.CountAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_ConcurrentDifferentEventsForSameRate_CommitsBothInboxRows()
    {
        await fixture.ResetAsync();
        await using var firstScope = await fixture.CreateScopeAsync(
            new CountingExchangeMarketDataModule([TestHistory.BtcPoint]));
        await using var secondScope = await fixture.CreateScopeAsync(
            new CountingExchangeMarketDataModule([TestHistory.BtcPoint]));
        var first = firstScope.ServiceProvider.GetRequiredService<IHistoricalEventHandler>();
        var second = secondScope.ServiceProvider.GetRequiredService<IHistoricalEventHandler>();

        await Task.WhenAll(
            first.HandleAsync(TestEvents.BtcChanged, TestContext.Current.CancellationToken),
            second.HandleAsync(
                TestEvents.BtcChanged with
                {
                    EventId = Guid.Parse("30000000-0000-0000-0000-000000000002")
                },
                TestContext.Current.CancellationToken));

        await using var verification = await fixture.CreateDbContextAsync();
        Assert.Single(await verification.HistoricalFundingRates.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
        Assert.Equal(2, await verification.InboxMessages.CountAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_SaveFails_CommitsNeitherHistoryNorInbox()
    {
        await fixture.ResetAsync();
        await using var scope = await fixture.CreateScopeAsync(
            new CountingExchangeMarketDataModule([TestHistory.BtcPoint]),
            new ThrowingSaveChangesInterceptor());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<IHistoricalEventHandler>()
                .HandleAsync(TestEvents.BtcChanged, TestContext.Current.CancellationToken));

        await using var verification = await fixture.CreateDbContextAsync();
        Assert.Empty(await verification.HistoricalFundingRates.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
        Assert.Empty(await verification.InboxMessages.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ConsumeAsync_DuplicateEventId_IsSkippedByInbox()
    {
        await fixture.ResetAsync();
        var exchangeModule = new CountingExchangeMarketDataModule([TestHistory.BtcPoint]);
        await using var scope = await fixture.CreateScopeAsync(exchangeModule);
        var handler = scope.ServiceProvider.GetRequiredService<IHistoricalEventHandler>();
        await handler.HandleAsync(TestEvents.BtcChanged, TestContext.Current.CancellationToken);
        await handler.HandleAsync(TestEvents.BtcChanged, TestContext.Current.CancellationToken);
        Assert.Equal(1, exchangeModule.HistoricalRequestCount);
    }

    [Fact]
    public async Task ConsumeAsync_NewEventIdForSamePair_IsProcessed()
    {
        await fixture.ResetAsync();
        var exchangeModule = new CountingExchangeMarketDataModule([TestHistory.BtcPoint]);
        await using var scope = await fixture.CreateScopeAsync(exchangeModule);
        var handler = scope.ServiceProvider.GetRequiredService<IHistoricalEventHandler>();
        await handler.HandleAsync(TestEvents.BtcChanged, TestContext.Current.CancellationToken);
        await handler.HandleAsync(
            TestEvents.BtcChanged with { EventId = Guid.NewGuid() },
            TestContext.Current.CancellationToken);
        Assert.Equal(2, exchangeModule.HistoricalRequestCount);
        await using var verification = await fixture.CreateDbContextAsync();
        Assert.Equal(2, await verification.InboxMessages.CountAsync(
            TestContext.Current.CancellationToken));
    }

    private sealed class ThrowingSaveChangesInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromException<InterceptionResult<int>>(
                new InvalidOperationException("Simulated save failure."));
    }
}
