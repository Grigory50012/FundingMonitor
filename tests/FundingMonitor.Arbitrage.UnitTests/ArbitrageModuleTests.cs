using FundingMonitor.Arbitrage.Application;
using FundingMonitor.Arbitrage.Contracts;
using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Options;
using Xunit;

namespace FundingMonitor.Arbitrage.UnitTests;

public sealed class ArbitrageModuleTests
{
    [Fact]
    public async Task GetOpportunitiesAsync_TwoFreshExchanges_ReturnsCalculatedOpportunity()
    {
        var current = new StubCurrentRatesModule(
        [
            TestCurrentRate.Create("Binance", "BTC-USDT", 100m, 0.001m, 109.5m, true),
            TestCurrentRate.Create("Bybit", "BTC-USDT", 99m, -0.001m, -109.5m, true)
        ]);
        var sut = new ArbitrageModuleService(current, Options.Create(new FundingArbitrageOptions
        {
            MinSpreadPercent = 10m
        }));

        var result = await sut.GetOpportunitiesAsync(
            new ArbitrageQuery("BTC", null), TestContext.Current.CancellationToken);

        var opportunity = Assert.Single(result);
        Assert.Equal(109.5m, opportunity.AprA);
        Assert.Equal(-109.5m, opportunity.AprB);
        Assert.Equal(219m, opportunity.AprSpread);
        Assert.Equal(ExchangeId.Parse("Binance"), opportunity.ShortExchange);
        Assert.Equal(ExchangeId.Parse("Bybit"), opportunity.LongExchange);
        Assert.Equal(1m, opportunity.PriceSpread);
    }

    [Fact]
    public async Task GetOpportunitiesAsync_InactiveRate_ReturnsEmpty()
    {
        var current = new StubCurrentRatesModule(
            [TestCurrentRate.Create("Binance", "BTC-USDT", 100m, 0.001m, 109.5m, false)]);
        var sut = new ArbitrageModuleService(current, Options.Create(new FundingArbitrageOptions()));

        var result = await sut.GetOpportunitiesAsync(
            new ArbitrageQuery(null, null), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetOpportunitiesAsync_StaleRate_ReturnsEmpty()
    {
        var current = new StubCurrentRatesModule(
        [
            TestCurrentRate.Create(
                "Binance",
                "BTC-USDT",
                100m,
                0.001m,
                109.5m,
                true,
                DateTimeOffset.UtcNow.AddMinutes(-6)),
            TestCurrentRate.Create("Bybit", "BTC-USDT", 99m, -0.001m, -109.5m, true)
        ]);
        var sut = new ArbitrageModuleService(current, Options.Create(new FundingArbitrageOptions()));

        var result = await sut.GetOpportunitiesAsync(
            new ArbitrageQuery(null, null), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetOpportunitiesAsync_ResultCannotBeMutated()
    {
        var current = new StubCurrentRatesModule(
        [
            TestCurrentRate.Create("Binance", "BTC-USDT", 100m, 0.001m, 109.5m, true),
            TestCurrentRate.Create("Bybit", "BTC-USDT", 99m, -0.001m, -109.5m, true)
        ]);
        var sut = new ArbitrageModuleService(current, Options.Create(new FundingArbitrageOptions()));

        var result = await sut.GetOpportunitiesAsync(
            new ArbitrageQuery(null, null), TestContext.Current.CancellationToken);

        var list = Assert.IsAssignableFrom<IList<ArbitrageOpportunityView>>(result);
        Assert.Throws<NotSupportedException>(() => list[0] = list[0]);
    }

    private sealed class StubCurrentRatesModule(IReadOnlyList<CurrentRateView> rates) : ICurrentRatesModule
    {
        public Task<IReadOnlyList<CurrentRateView>> GetCurrentRatesAsync(
            CurrentRatesQuery query,
            CancellationToken cancellationToken) => Task.FromResult(rates);
    }

    private static class TestCurrentRate
    {
        public static CurrentRateView Create(
            string exchange,
            string symbol,
            decimal markPrice,
            decimal fundingRate,
            decimal apr,
            bool isActive,
            DateTimeOffset? lastSeenOnUtc = null) => new(
            ExchangeId.Parse(exchange),
            Symbol.Create(symbol),
            markPrice,
            markPrice,
            fundingRate,
            8,
            DateTimeOffset.UtcNow.AddHours(1),
            null,
            lastSeenOnUtc ?? DateTimeOffset.UtcNow,
            isActive,
            apr,
            0m);
    }
}
