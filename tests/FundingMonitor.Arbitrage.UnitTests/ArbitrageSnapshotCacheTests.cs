using FundingMonitor.Arbitrage.Application;
using FundingMonitor.Arbitrage.Domain;
using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.SharedKernel;
using Xunit;

namespace FundingMonitor.Arbitrage.UnitTests;

public sealed class ArbitrageSnapshotCacheTests
{
    [Fact]
    public void SameSnapshot_ReusesCalculationRegardlessOfInputOrder()
    {
        var cache = new ArbitrageSnapshotCache();
        var rates = new[] { Rate("Bybit"), Rate("Binance") };
        var calls = 0;
        IReadOnlyList<FundingArbitrageOpportunity> Calculate(IReadOnlyList<CurrentRateView> _) { calls++; return []; }

        var first = cache.GetOrCalculate(rates, Calculate);
        var second = cache.GetOrCalculate(rates.Reverse().Select(rate => rate with { }).ToArray(), Calculate);

        Assert.Same(first, second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void ChangedPriceOrRemovedRate_Recalculates()
    {
        var cache = new ArbitrageSnapshotCache();
        var rates = new[] { Rate("Binance"), Rate("Bybit") };
        var calls = 0;
        IReadOnlyList<FundingArbitrageOpportunity> Calculate(IReadOnlyList<CurrentRateView> _) { calls++; return []; }
        cache.GetOrCalculate(rates, Calculate);
        rates[0] = rates[0] with { MarkPrice = 102m };
        cache.GetOrCalculate(rates, Calculate);
        cache.GetOrCalculate([rates[0]], Calculate);
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task ConcurrentRequests_CalculateSnapshotOnce()
    {
        var cache = new ArbitrageSnapshotCache();
        var rates = new[] { Rate("Binance"), Rate("Bybit") };
        var calls = 0;
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
            cache.GetOrCalculate(rates, _ => { Interlocked.Increment(ref calls); return []; }),
            TestContext.Current.CancellationToken)));
        Assert.Equal(1, calls);
    }

    private static CurrentRateView Rate(string exchange) => new(
        ExchangeId.Parse(exchange), Symbol.Create("BTC-USDT"), 100m, 100m, 0.001m,
        8, null, new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero), true, 109.5m, 0m);
}
