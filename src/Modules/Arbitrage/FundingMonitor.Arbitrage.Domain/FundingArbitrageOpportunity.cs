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
    public decimal PriceSpreadPercent =>
        PriceB > 0m ? Math.Abs(PriceA - PriceB) / PriceB * 100m : 0m;
    public required decimal FundingRateA { get; init; }
    public required decimal FundingRateB { get; init; }
    public decimal FundingRateSpread => FundingRateA - FundingRateB;
    public required decimal AprA { get; init; }
    public required decimal AprB { get; init; }
    public decimal AprSpread => Math.Abs(AprA - AprB);
    public required int PaymentsA { get; init; }
    public required int PaymentsB { get; init; }
    public ExchangeId ShortExchange => FundingRateA > FundingRateB ? ExchangeA : ExchangeB;
    public ExchangeId LongExchange => FundingRateA <= FundingRateB ? ExchangeA : ExchangeB;
}
