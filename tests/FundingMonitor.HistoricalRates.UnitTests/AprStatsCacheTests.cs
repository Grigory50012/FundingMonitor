using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.HistoricalRates.Application;
using FundingMonitor.HistoricalRates.Application.Persistence;
using FundingMonitor.HistoricalRates.Contracts;
using FundingMonitor.HistoricalRates.Domain;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace FundingMonitor.HistoricalRates.UnitTests;

public sealed class AprStatsCacheTests
{
    [Fact]
    public void InvalidationTokens_RespectCacheSizeLimit()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 2 });
        var invalidation = new AprStatsCacheInvalidation(cache);
        for (var index = 0; index < 100; index++)
            invalidation.GetToken(Symbol.Create($"TEST{index}-USDT"));
        Assert.InRange(cache.Count, 0, 2);
    }

    [Fact]
    public void InvalidatedToken_RejectsLateCachePublication()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
        var invalidation = new AprStatsCacheInvalidation(cache);
        var symbol = Symbol.Create("BTC-USDT");
        using (var entry = cache.CreateEntry("in-flight-apr"))
        {
            entry.Size = 1;
            entry.AddExpirationToken(invalidation.GetToken(symbol));
            invalidation.Invalidate(symbol);
            entry.Value = "stale result";
        }
        Assert.False(cache.TryGetValue("in-flight-apr", out _));
        Assert.False(invalidation.GetToken(symbol).HasChanged);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SavedHistory_InvalidatesSymbolAcrossExchangeFilters(bool filterExchange)
    {
        var symbol = Symbol.Create("BTC-USDT");
        var otherSymbol = Symbol.Create("ETH-USDT");
        var exchange = ExchangeId.Parse("Binance");
        var store = new Store();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
        var invalidation = new AprStatsCacheInvalidation(cache);
        var service = new HistoricalRatesService(store, cache,
            Options.Create(new AprStatsOptions { Periods = [1] }), TimeProvider.System, invalidation);
        IReadOnlyCollection<ExchangeId>? exchanges = filterExchange ? [exchange] : null;
        var token = TestContext.Current.CancellationToken;
        var before = await service.GetAprStatsAsync(symbol, exchanges, token);
        var other = await service.GetAprStatsAsync(otherSymbol, null, token);
        var handler = new HistoricalEventHandler(new Market(), store,
            Options.Create(new HistoricalDataCollectionOptions()), TimeProvider.System, invalidation);

        await handler.HandleAsync(new HistoricalCollectionRequest(Guid.NewGuid(), exchange, symbol), token);

        var after = await service.GetAprStatsAsync(symbol, exchanges, token);
        Assert.Equal(36.5m, Assert.Single(before).Apr);
        Assert.Equal(73m, Assert.Single(after).Apr);
        Assert.Same(other, await service.GetAprStatsAsync(otherSymbol, null, token));
    }

    [Fact]
    public async Task FailedWrite_DoesNotInvalidatePreviouslyCachedStats()
    {
        var symbol = Symbol.Create("BTC-USDT");
        var store = new Store { FailWrite = true };
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
        var invalidation = new AprStatsCacheInvalidation(cache);
        var service = new HistoricalRatesService(store, cache,
            Options.Create(new AprStatsOptions { Periods = [1] }), TimeProvider.System, invalidation);
        var token = TestContext.Current.CancellationToken;
        var before = await service.GetAprStatsAsync(symbol, null, token);
        var handler = new HistoricalEventHandler(new Market(), store,
            Options.Create(new HistoricalDataCollectionOptions()), TimeProvider.System, invalidation);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new HistoricalCollectionRequest(Guid.NewGuid(), ExchangeId.Parse("Binance"), symbol), token));

        Assert.Same(before, await service.GetAprStatsAsync(symbol, null, token));
    }

    private sealed class Store : IHistoricalRatesRepository, IHistoricalRatesUnitOfWork
    {
        private decimal rate = 0.001m;
        public bool FailWrite { get; init; }

        public Task<IReadOnlyList<HistoricalFundingRate>> GetHistoryAsync(
            HistoricalRatesQuery query, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<HistoricalFundingRate>>([
                new(ExchangeId.Parse("Binance"), query.Symbol, rate,
                    DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
            ]);

        public Task StoreHistoryAsync(IReadOnlyCollection<HistoricalFundingRate> rates,
            CancellationToken cancellationToken)
        {
            if (FailWrite) throw new InvalidOperationException("Write failed");
            rate = rates.Single().FundingRate;
            return Task.CompletedTask;
        }
    }

    private sealed class Market : IExchangeMarketDataModule
    {
        public Task<Result<IReadOnlyList<HistoricalRatePoint>>> GetHistoricalRatesAsync(
            HistoricalRatesRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(Result<IReadOnlyList<HistoricalRatePoint>>.Success([
                new(request.Exchange, request.Symbol, 0.002m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
            ]));
        public Task<Result<IReadOnlyList<CurrentRateSnapshot>>> GetCurrentRatesAsync(
            ExchangeId exchange, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<ExchangeHealthSnapshot>> CheckHealthAsync(
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
