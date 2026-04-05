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

    public required decimal APRFundingRateA { get; init; }
    public required decimal APRFundingRateB { get; init; }
    public decimal APRSpread => APRFundingRateA - APRFundingRateB;

    public decimal ProfitabilityPercent => Math.Abs(APRSpread);

    public required int PaymentsA { get; init; }
    public required int PaymentsB { get; init; }

    public ExchangeType ShortExchange => APRFundingRateA > APRFundingRateB ? ExchangeA : ExchangeB;
    public ExchangeType LongExchange => APRFundingRateA <= APRFundingRateB ? ExchangeA : ExchangeB;
}