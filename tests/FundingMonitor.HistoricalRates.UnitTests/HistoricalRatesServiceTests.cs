using FundingMonitor.HistoricalRates.Application;
using FundingMonitor.HistoricalRates.Application.Persistence;
using FundingMonitor.HistoricalRates.Contracts;
using FundingMonitor.HistoricalRates.Domain;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace FundingMonitor.HistoricalRates.UnitTests;

public sealed class HistoricalRatesServiceTests
{
    [Fact]
    public async Task GetAprStatsAsync_TwoPayments_PreservesLegacyFormula()
    {
        var repository = new StubRepository(
        [
            CreateRate(0.001m, 8),
            CreateRate(0.002m, 16)
        ]);
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
        var service = new HistoricalRatesService(
            repository,
            cache,
            Options.Create(new AprStatsOptions { Periods = [1] }),
            new FixedTimeProvider());

        var result = await service.GetAprStatsAsync(
            Symbol.Create("BTC-USDT"),
            null,
            TestContext.Current.CancellationToken);

        var stats = Assert.Single(result);
        Assert.Equal(109.5m, stats.Apr);
        Assert.Equal(0.3m, stats.TotalFundingRatePercent);
        Assert.Equal(2, stats.PaymentsCount);
        Assert.Equal(0.15m, stats.AvgFundingRatePercent);
        Assert.Equal(0.05m, stats.StdDev);
    }

    private static HistoricalFundingRate CreateRate(decimal rate, int hour) => new(
        ExchangeId.Parse("Binance"),
        Symbol.Create("BTC-USDT"),
        rate,
        new DateTimeOffset(2026, 9, 8, hour, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 9, 8, 18, 0, 0, TimeSpan.Zero));

    private sealed class StubRepository(IReadOnlyList<HistoricalFundingRate> rates)
        : IHistoricalRatesRepository
    {
        public Task<IReadOnlyList<HistoricalFundingRate>> GetHistoryAsync(
            HistoricalRatesQuery query,
            CancellationToken cancellationToken) => Task.FromResult(rates);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            new(2026, 9, 8, 18, 0, 0, TimeSpan.Zero);
    }
}
