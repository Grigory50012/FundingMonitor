using FundingMonitor.SharedKernel;

namespace FundingMonitor.Arbitrage.Contracts;

public sealed record ArbitrageOpportunityView(
    Symbol Symbol,
    ExchangeId ExchangeA,
    ExchangeId ExchangeB,
    decimal BidPriceA,
    decimal AskPriceA,
    decimal BidPriceB,
    decimal AskPriceB,
    decimal PriceSpread,
    decimal PriceSpreadPercent,
    decimal FundingRateA,
    decimal FundingRateB,
    decimal FundingRateSpread,
    decimal AprA,
    decimal AprB,
    decimal? AprSpread,
    int? FundingIntervalHoursA,
    int? FundingIntervalHoursB,
    int PaymentsA,
    int PaymentsB,
    ExchangeId? ShortExchange,
    ExchangeId? LongExchange);
