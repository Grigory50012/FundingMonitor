using FundingMonitor.SharedKernel;

namespace FundingMonitor.Arbitrage.Domain;

public sealed record FundingArbitrageOpportunity
{
    public required Symbol Symbol { get; init; }
    public required ExchangeId ExchangeA { get; init; }
    public required ExchangeId ExchangeB { get; init; }
    public required decimal PriceA { get; init; }
    public required decimal PriceB { get; init; }
    public decimal PriceSpread => PriceA - PriceB;
    public decimal? PriceSpreadPercent
    {
        get
        {
            if (ShortExchange is not { } shortExchange || PriceA <= 0m || PriceB <= 0m)
                return null;

            var signedSpread = shortExchange == ExchangeA ? PriceSpread : -PriceSpread;
            return signedSpread / ((PriceA + PriceB) / 2m) * 100m;
        }
    }
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
