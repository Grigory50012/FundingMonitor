using FundingMonitor.SharedKernel;

namespace FundingMonitor.Arbitrage.Calculation;

internal sealed record FundingArbitrageOpportunity
{
    public required Symbol Symbol { get; init; }
    public required ExchangeId ExchangeA { get; init; }
    public required ExchangeId ExchangeB { get; init; }
    public required decimal BidPriceA { get; init; }
    public required decimal AskPriceA { get; init; }
    public required decimal BidPriceB { get; init; }
    public required decimal AskPriceB { get; init; }
    private decimal BidShort => ShortExchange == ExchangeA ? BidPriceA : BidPriceB;
    private decimal AskLong => ShortExchange == ExchangeA ? AskPriceB : AskPriceA;
    public decimal PriceSpread => BidShort - AskLong;
    public decimal PriceSpreadPercent => 100m * PriceSpread / ((BidShort + AskLong) / 2m);
    public required decimal FundingRateA { get; init; }
    public required decimal FundingRateB { get; init; }
    public decimal FundingRateSpread => FundingRateA - FundingRateB;
    public required decimal AprA { get; init; }
    public required decimal AprB { get; init; }
    public int? FundingIntervalHoursA { get; init; }
    public int? FundingIntervalHoursB { get; init; }
    private bool HasKnownIntervals => FundingIntervalHoursA is > 0 && FundingIntervalHoursB is > 0;
    public decimal? AprSpread => HasKnownIntervals ? Math.Abs(AprA - AprB) : null;
    public required int PaymentsA { get; init; }
    public required int PaymentsB { get; init; }
    public ExchangeId? ShortExchange => HasKnownIntervals
        ? (AprA > AprB || (AprA == AprB && string.CompareOrdinal(ExchangeA.Value, ExchangeB.Value) < 0)
            ? ExchangeA : ExchangeB)
        : null;

    public ExchangeId? LongExchange => ShortExchange is { } shortExchange
        ? (shortExchange == ExchangeA ? ExchangeB : ExchangeA)
        : null;
}
