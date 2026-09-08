namespace FundingMonitor.Arbitrage.Presentation.Dtos;

public sealed record FundingArbitrageDto(
    string Symbol,
    string ExchangeA,
    string ExchangeB,
    decimal PriceA,
    decimal PriceB,
    decimal PriceSpread,
    decimal PriceSpreadPercent,
    decimal FundingRateA,
    decimal FundingRateB,
    decimal FundingRateSpread,
    decimal AprA,
    decimal AprB,
    decimal AprSpread,
    int PaymentsA,
    int PaymentsB,
    string ShortExchange,
    string LongExchange,
    string ExchangeAUrl,
    string ExchangeBUrl);
