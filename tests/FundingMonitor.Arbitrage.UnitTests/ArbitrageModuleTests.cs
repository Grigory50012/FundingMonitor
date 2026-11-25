using FundingMonitor.Arbitrage.Calculation;
using FundingMonitor.Arbitrage.GetOpportunities;
using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace FundingMonitor.Arbitrage.UnitTests;

public sealed class ArbitrageModuleTests
{
    [Fact]
    public async Task SeparateRequests_RecalculatePairsInStableOrder()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentRatesModule>(new StubCurrentRatesModule([
            TestCurrentRate.Create("Bybit", "BTC-USDT", 99m, -0.001m, -109.5m, true),
            TestCurrentRate.Create("Binance", "BTC-USDT", 100m, 0.001m, 109.5m, true)
        ]));
        services.AddArbitrageModule(new ConfigurationManager());
        using var provider = services.BuildServiceProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var token = TestContext.Current.CancellationToken;

        var first = await firstScope.ServiceProvider.GetRequiredService<FundingArbitrageDetector>().DetectAsync(token);
        var second = await secondScope.ServiceProvider.GetRequiredService<FundingArbitrageDetector>().DetectAsync(token);

        Assert.NotSame(first, second);
        var pair = Assert.Single(first);
        Assert.Equal(ExchangeId.Parse("Binance"), pair.ExchangeA);
        Assert.Equal(ExchangeId.Parse("Bybit"), pair.ExchangeB);
        Assert.Equal(pair, Assert.Single(second));
    }

    [Fact]
    public async Task DetectAsync_DropsRatesWhenTheyBecomeStale()
    {
        var time = new MutableTimeProvider { Now = DateTimeOffset.UtcNow };
        var current = new StubCurrentRatesModule([
            TestCurrentRate.Create("Binance", "BTC-USDT", 100m, 0.001m, 109.5m, true, time.Now),
            TestCurrentRate.Create("Bybit", "BTC-USDT", 99m, -0.001m, -109.5m, true, time.Now)
        ]);
        var detector = new FundingArbitrageDetector(current, Options.Create(new FundingArbitrageOptions()),
            time);
        var token = TestContext.Current.CancellationToken;
        Assert.Single(await detector.DetectAsync(token));

        time.Now = time.Now.AddMinutes(6);

        Assert.Empty(await detector.DetectAsync(token));
    }

    [Fact]
    public async Task DetectAsync_ExcludesOnlyIncompleteLegsAndExpiresAtPayment()
    {
        var time = new MutableTimeProvider { Now = DateTimeOffset.UtcNow };
        var payment = time.Now.AddSeconds(10);
        var current = new StubCurrentRatesModule([
            TestCurrentRate.Create("Binance", "BTC-USDT", 100m, 0m, 0m, true, time.Now)
                with { NextFundingTime = payment },
            TestCurrentRate.Create("Bybit", "BTC-USDT", 99m, -0.001m, -109.5m, true, time.Now),
            TestCurrentRate.Create("OKX", "BTC-USDT", 101m, 0.001m, 109.5m, true, time.Now)
                with { AskPrice = null }
        ]);
        var detector = new FundingArbitrageDetector(current, Options.Create(new FundingArbitrageOptions()), time);
        var token = TestContext.Current.CancellationToken;

        var pair = Assert.Single(await detector.DetectAsync(token));
        Assert.Equal(ExchangeId.Parse("Binance"), pair.ShortExchange);
        Assert.Equal(ExchangeId.Parse("Bybit"), pair.LongExchange);

        time.Now = payment;
        Assert.Empty(await detector.DetectAsync(token));
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; }
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public void AddArbitrageModule_ResolvesGetOpportunitiesHandler()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentRatesModule>(new StubCurrentRatesModule([]));
        services.AddArbitrageModule(new ConfigurationManager());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService<GetOpportunitiesHandler>();

        Assert.NotNull(handler);
    }

    [Fact]
    public async Task HandleAsync_TwoFreshExchanges_ReturnsCalculatedOpportunity()
    {
        var current = new StubCurrentRatesModule(
        [
            TestCurrentRate.Create("Binance", "BTC-USDT", 100m, 0.001m, 109.5m, true),
            TestCurrentRate.Create("Bybit", "BTC-USDT", 99m, -0.001m, -109.5m, true)
        ]);
        var handler = CreateHandler(current, new FundingArbitrageOptions());

        var result = await handler.HandleAsync(
            new GetOpportunitiesQuery("BTC", null), TestContext.Current.CancellationToken);

        var opportunity = Assert.Single(result);
        Assert.Equal(109.5m, opportunity.AprA);
        Assert.Equal(-109.5m, opportunity.AprB);
        Assert.Equal(219m, opportunity.AprSpread);
        Assert.Equal("Binance", opportunity.ShortExchange);
        Assert.Equal("Bybit", opportunity.LongExchange);
        Assert.Equal(1m, opportunity.PriceSpread);
    }

    [Fact]
    public async Task HandleAsync_InactiveRate_ReturnsEmpty()
    {
        var current = new StubCurrentRatesModule(
        [
            TestCurrentRate.Create("Binance", "BTC-USDT", 100m, 0.001m, 109.5m, false),
            TestCurrentRate.Create("Bybit", "BTC-USDT", 99m, -0.001m, -109.5m, true)
        ]);
        var handler = CreateHandler(current, new FundingArbitrageOptions());

        var result = await handler.HandleAsync(
            new GetOpportunitiesQuery(null, null), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_StaleRate_ReturnsEmpty()
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
        var handler = CreateHandler(current, new FundingArbitrageOptions());

        var result = await handler.HandleAsync(
            new GetOpportunitiesQuery(null, null), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_ResultCannotBeMutated()
    {
        var current = new StubCurrentRatesModule(
        [
            TestCurrentRate.Create("Binance", "BTC-USDT", 100m, 0.001m, 109.5m, true),
            TestCurrentRate.Create("Bybit", "BTC-USDT", 99m, -0.001m, -109.5m, true)
        ]);
        var handler = CreateHandler(current, new FundingArbitrageOptions());

        var result = await handler.HandleAsync(
            new GetOpportunitiesQuery(null, null), TestContext.Current.CancellationToken);

        var list = Assert.IsAssignableFrom<IList<FundingArbitrageDto>>(result);
        Assert.Throws<NotSupportedException>(() => list[0] = list[0]);
    }

    [Fact]
    public async Task HandleAsync_AppliesAllFiltersAndInclusiveBoundaries()
    {
        var current = new StubCurrentRatesModule(
        [
            TestCurrentRate.Create("Binance", "BTC-USDT", 99m, 0.00002m, 3m, true),
            TestCurrentRate.Create("Bybit", "BTC-USDT", 101m, 0.00001m, 1m, true),
            TestCurrentRate.Create("OKX", "BTC-USDT", 101m, 0.00001m, 1m, true, intervalHours: 4),
            TestCurrentRate.Create("Binance", "ETH-USDT", 99m, 0.00002m, 3m, true),
            TestCurrentRate.Create("Bybit", "ETH-USDT", 101m, 0.00001m, 1m, true)
        ]);
        var handler = CreateHandler(current, new FundingArbitrageOptions());
        var query = new GetOpportunitiesQuery("btc", [ExchangeId.Parse("Binance"), ExchangeId.Parse("Bybit")],
            MinAprPercent: 2m, MaxAdversePriceSpreadPercent: 2m, SameFundingIntervalOnly: true);
        var token = TestContext.Current.CancellationToken;

        Assert.Equal(-2m, Assert.Single(await handler.HandleAsync(query, token)).PriceSpreadPercent);
        Assert.Empty(await handler.HandleAsync(query with { MinAprPercent = 2.1m }, token));
        Assert.Empty(await handler.HandleAsync(query with { MaxAdversePriceSpreadPercent = 1.9m }, token));
        var mixed = query with { Exchanges = [ExchangeId.Parse("Binance"), ExchangeId.Parse("OKX")] };
        Assert.Empty(await handler.HandleAsync(mixed, token));
        Assert.Single(await handler.HandleAsync(mixed with { SameFundingIntervalOnly = false }, token));
        Assert.Equal(4, (await handler.HandleAsync(new GetOpportunitiesQuery(null, null), token)).Count);
    }

    [Fact]
    public async Task HandleAsync_ZeroPriceLimitKeepsFavorableSpreads()
    {
        var current = new StubCurrentRatesModule(
        [
            TestCurrentRate.Create("Binance", "BTC-USDT", 99m, 0.00001m, 1m, true),
            TestCurrentRate.Create("Bybit", "BTC-USDT", 101m, 0.00002m, 3m, true),
            TestCurrentRate.Create("OKX", "BTC-USDT", 0m, 0.00003m, 4m, true)
        ]);
        var handler = CreateHandler(current, new FundingArbitrageOptions());
        var query = new GetOpportunitiesQuery(null, null, MaxAdversePriceSpreadPercent: 0m);

        var match = Assert.Single(await handler.HandleAsync(query, TestContext.Current.CancellationToken));
        Assert.Equal(2m, match.PriceSpreadPercent);
    }

    [Fact]
    public async Task HandleAsync_UnknownIntervalIsExcluded()
    {
        var current = new StubCurrentRatesModule(
        [
            TestCurrentRate.Create("Binance", "BTC-USDT", 101m, 0.00002m, 3m, true),
            TestCurrentRate.Create("Bybit", "BTC-USDT", 99m, 0.00001m, 0m, true, intervalHours: null)
        ]);
        var handler = CreateHandler(current, new FundingArbitrageOptions());
        var token = TestContext.Current.CancellationToken;
        var query = new GetOpportunitiesQuery(null, null);

        Assert.Empty(await handler.HandleAsync(query, token));
        Assert.Empty(await handler.HandleAsync(query with { MinAprPercent = 0m }, token));
        Assert.Empty(await handler.HandleAsync(query with { MaxAdversePriceSpreadPercent = 0m }, token));
        Assert.Empty(await handler.HandleAsync(query with { SameFundingIntervalOnly = true }, token));
    }

    private sealed class StubCurrentRatesModule(IReadOnlyList<CurrentRateView> rates) : ICurrentRatesModule
    {
        public Task<IReadOnlyList<CurrentRateView>> GetCurrentRatesAsync(
            CurrentRatesQuery query,
            CancellationToken cancellationToken) => Task.FromResult(rates);
    }

    private static GetOpportunitiesHandler CreateHandler(
        ICurrentRatesModule currentRates,
        FundingArbitrageOptions options) =>
        new(
            new FundingArbitrageDetector(currentRates, Options.Create(options), TimeProvider.System));

    private static class TestCurrentRate
    {
        public static CurrentRateView Create(
            string exchange,
            string symbol,
            decimal markPrice,
            decimal fundingRate,
            decimal apr,
            bool isActive,
            DateTimeOffset? lastSeenOnUtc = null,
            int? intervalHours = 8) => new(
            ExchangeId.Parse(exchange),
            Symbol.Create(symbol),
            markPrice,
            markPrice,
            fundingRate,
            intervalHours,
            DateTimeOffset.UtcNow.AddHours(1),
            lastSeenOnUtc ?? DateTimeOffset.UtcNow,
            isActive,
            apr,
            0m) { BidPrice = markPrice, AskPrice = markPrice };
    }
}
