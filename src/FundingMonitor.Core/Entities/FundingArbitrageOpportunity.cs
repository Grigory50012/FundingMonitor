namespace FundingMonitor.Core.Entities;

/// <summary>
///     Арбитражная возможность по ставке финансирования
/// </summary>
public record FundingArbitrageOpportunity
{
    public required string Symbol { get; init; }

    public required ExchangeType ExchangeA { get; init; }
    public required ExchangeType ExchangeB { get; init; }

    public required decimal PriceA { get; init; }
    public required decimal PriceB { get; init; }
    public decimal PriceSpread => PriceA - PriceB;
    public decimal PriceSpreadPercent => PriceB > 0 ? Math.Abs(PriceA - PriceB) / PriceB * 100m : 0m;

    public required decimal FundingRateA { get; init; }
    public required decimal FundingRateB { get; init; }
    public decimal FundingRateSpread => FundingRateA - FundingRateB;

    public decimal ProfitabilityPercent => Math.Abs(FundingRateSpread);

    public required int PaymentsA { get; init; }
    public required int PaymentsB { get; init; }

    public ExchangeType ShortExchange => FundingRateA > FundingRateB ? ExchangeA : ExchangeB;
    public ExchangeType LongExchange => FundingRateA <= FundingRateB ? ExchangeA : ExchangeB;
}