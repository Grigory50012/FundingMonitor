using FundingMonitor.SharedKernel;

namespace FundingMonitor.Arbitrage.Contracts;

public sealed record ArbitrageOpportunityView(
    Symbol Symbol,
    ExchangeId ExchangeA,
    ExchangeId ExchangeB,
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
    ExchangeId ShortExchange,
    ExchangeId LongExchange);
