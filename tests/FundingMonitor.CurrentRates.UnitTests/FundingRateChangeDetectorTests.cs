using FundingMonitor.CurrentRates.Application.Collection;
using FundingMonitor.CurrentRates.Domain;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FundingMonitor.CurrentRates.UnitTests;

public sealed class FundingRateChangeDetectorTests
{
    [Fact]
    public void DetectChanges_NewAndChangedSymbols_ProducesDistinctIntegrationEvents()
    {
        var occurredOn = new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);
        var btc = Symbol.Create("BTC-USDT");
        var eth = Symbol.Create("ETH-USDT");
        var previous = new Dictionary<Symbol, CurrentFundingRate>
        {
            [btc] = CreateRate(btc, occurredOn.AddHours(1))
        };
        var current = new[]
        {
            CreateRate(btc, occurredOn.AddHours(2)),
            CreateRate(eth, occurredOn.AddHours(1))
        };

        var events = new FundingRateChangeDetector(
            NullLogger<FundingRateChangeDetector>.Instance).DetectChanges(
                ExchangeId.Parse("Binance"), previous, current, occurredOn);

        Assert.Equal(2, events.Count);
        Assert.Equal(2, events.Select(item => item.EventId).Distinct().Count());
        Assert.DoesNotContain(events, item => item.EventId == Guid.Empty);
        Assert.All(events, item => Assert.Equal(occurredOn, item.OccurredOnUtc));
    }

    internal static CurrentFundingRate CreateRate(Symbol symbol, DateTimeOffset? nextFundingTime) =>
        new()
        {
            Exchange = ExchangeId.Parse("Binance"),
            Symbol = symbol,
            BaseAsset = symbol.Value.Split('-')[0],
            QuoteAsset = symbol.Value.Split('-')[1],
            MarkPrice = 100m,
            IndexPrice = 100m,
            FundingRate = 0.0001m,
            FundingIntervalHours = 8,
            NextFundingTime = nextFundingTime,
            LastSeenOnUtc = new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero)
        };
}
