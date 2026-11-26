using FundingMonitor.CurrentRates;
using FundingMonitor.CurrentRates.IntegrationTests.Infrastructure;
using FundingMonitor.CurrentRates.Persistence;
using FundingMonitor.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FundingMonitor.CurrentRates.IntegrationTests.Outbox;

[Collection("CurrentRatesPostgreSql")]
public sealed class CurrentRatesOutboxTests(CurrentRatesPostgreSqlFixture fixture)
{
    [Fact]
    public async Task StoreSnapshotAsync_RateAndEvent_CommitsBoth()
    {
        await fixture.ResetAsync();
        await using var scope = await fixture.CreateScopeAsync();
        var repository = scope.ServiceProvider.GetRequiredService<ICurrentRatesRepository>();

        await repository.StoreSnapshotAsync(
            ExchangeId.Parse("Binance"),
            [TestRate],
            TestContext.Current.CancellationToken);

        await using var verification = await fixture.CreateDbContextAsync();
        Assert.Single(await verification.CurrentFundingRates.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
        var outbox = Assert.Single(await verification.OutboxMessages.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
        Assert.Null(outbox.ProcessedOnUtc);
        Assert.NotEqual(Guid.Empty, outbox.Id);
        Assert.Equal(TestRate.Exchange.Value, outbox.Exchange);
        Assert.Equal(TestRate.Symbol.Value, outbox.Symbol);
        Assert.Equal(TestRate.NextFundingTime, outbox.NextFundingTime);
    }

    [Fact]
    public async Task StoreSnapshotAsync_SaveFails_CommitsNeitherRateNorEvent()
    {
        await fixture.ResetAsync();
        await using var scope = await fixture.CreateScopeAsync(new ThrowingSaveChangesInterceptor());
        var repository = scope.ServiceProvider.GetRequiredService<ICurrentRatesRepository>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.StoreSnapshotAsync(
            ExchangeId.Parse("Binance"),
            [TestRate],
            TestContext.Current.CancellationToken));

        await using var verification = await fixture.CreateDbContextAsync();
        Assert.Empty(await verification.CurrentFundingRates.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
        Assert.Empty(await verification.OutboxMessages.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StoreSnapshotAsync_MissingStaleRate_DeactivatesIt()
    {
        await fixture.ResetAsync();
        await using (var firstScope = await fixture.CreateScopeAsync())
        {
            var repository = firstScope.ServiceProvider.GetRequiredService<ICurrentRatesRepository>();
            await repository.StoreSnapshotAsync(
                ExchangeId.Parse("Binance"),
                [TestRate],
                TestContext.Current.CancellationToken);
        }

        await using (var secondScope = await fixture.CreateScopeAsync())
        {
            var repository = secondScope.ServiceProvider.GetRequiredService<ICurrentRatesRepository>();
            await repository.StoreSnapshotAsync(
                ExchangeId.Parse("Binance"),
                [],
                TestContext.Current.CancellationToken);
        }

        await using var verification = await fixture.CreateDbContextAsync();
        var rate = Assert.Single(await verification.CurrentFundingRates.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
        Assert.False(rate.IsActive);
    }

    [Fact]
    public async Task StoreSnapshotAsync_FreshRateWithoutQuote_ClearsOldQuote()
    {
        await fixture.ResetAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var initial = TestRate with { BidPrice = 99m, AskPrice = 101m, FundingRate = 0m };
        await using var scope = await fixture.CreateScopeAsync();
        var repository = scope.ServiceProvider.GetRequiredService<ICurrentRatesRepository>();
        await repository.StoreSnapshotAsync(initial.Exchange, [initial], cancellationToken);

        await using var verification = await fixture.CreateDbContextAsync();
        var stored = await verification.CurrentFundingRates.AsNoTracking().SingleAsync(cancellationToken);
        Assert.Equal(99m, stored.BidPrice);
        Assert.Equal(101m, stored.AskPrice);
        Assert.Equal(0m, stored.FundingRate);

        var updated = initial with
        {
            BidPrice = null,
            AskPrice = null,
            FundingRate = null,
            LastSeenOnUtc = initial.LastSeenOnUtc.AddSeconds(10)
        };
        await repository.StoreSnapshotAsync(initial.Exchange, [updated], cancellationToken);

        stored = await verification.CurrentFundingRates.AsNoTracking().SingleAsync(cancellationToken);
        Assert.Null(stored.BidPrice);
        Assert.Null(stored.AskPrice);
        Assert.Null(stored.FundingRate);
        Assert.Equal(updated.LastSeenOnUtc, stored.LastSeenOnUtc);
        Assert.Equal(1, await verification.OutboxMessages.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task StoreSnapshotAsync_ConcurrentExchanges_KeepIndependentRatesAndEvents()
    {
        await fixture.ResetAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var binanceRate = TestRate;
        var okxRate = TestRate with { Exchange = ExchangeId.Parse("OKX") };
        await using var binanceScope = await fixture.CreateScopeAsync();
        await using var okxScope = await fixture.CreateScopeAsync();
        var binanceRepository = binanceScope.ServiceProvider.GetRequiredService<ICurrentRatesRepository>();
        var okxRepository = okxScope.ServiceProvider.GetRequiredService<ICurrentRatesRepository>();

        await Task.WhenAll(
            binanceRepository.StoreSnapshotAsync(binanceRate.Exchange, [binanceRate], cancellationToken),
            okxRepository.StoreSnapshotAsync(okxRate.Exchange, [okxRate], cancellationToken));

        var updatedOkxRate = okxRate with { NextFundingTime = okxRate.NextFundingTime!.Value.AddHours(4) };
        await Task.WhenAll(
            binanceRepository.StoreSnapshotAsync(binanceRate.Exchange, [], cancellationToken),
            okxRepository.StoreSnapshotAsync(okxRate.Exchange, [updatedOkxRate], cancellationToken));

        await using var verification = await fixture.CreateDbContextAsync();
        var rates = await verification.CurrentFundingRates.AsNoTracking().ToDictionaryAsync(
            rate => rate.Exchange, cancellationToken);
        Assert.Equal(2, rates.Count);
        Assert.False(rates["Binance"].IsActive);
        Assert.True(rates["OKX"].IsActive);
        Assert.Equal(updatedOkxRate.NextFundingTime, rates["OKX"].NextFundingTime);

        var outbox = await verification.OutboxMessages.AsNoTracking().ToListAsync(cancellationToken);
        Assert.Equal(3, outbox.Select(message => message.Id).Distinct().Count());
        Assert.Single(outbox, message => message.Exchange == "Binance");
        Assert.Single(outbox, message => message.Exchange == "OKX" && message.NextFundingTime == okxRate.NextFundingTime);
        Assert.Single(outbox, message => message.Exchange == "OKX" && message.NextFundingTime == updatedOkxRate.NextFundingTime);
    }

    private static CurrentFundingRate TestRate => new()
    {
        Exchange = ExchangeId.Parse("Binance"),
        Symbol = Symbol.Create("BTC-USDT"),
        BaseAsset = "BTC",
        QuoteAsset = "USDT",
        MarkPrice = 100m,
        IndexPrice = 100m,
        FundingRate = 0.0001m,
        FundingIntervalHours = 8,
        NextFundingTime = new DateTimeOffset(2026, 9, 6, 16, 0, 0, TimeSpan.Zero),
        LastSeenOnUtc = new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero)
    };

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

[CollectionDefinition("CurrentRatesPostgreSql")]
public sealed class CurrentRatesPostgreSqlCollection :
    ICollectionFixture<CurrentRatesPostgreSqlFixture>;
