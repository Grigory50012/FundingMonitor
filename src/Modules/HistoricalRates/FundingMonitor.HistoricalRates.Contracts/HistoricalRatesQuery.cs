using FundingMonitor.SharedKernel;

namespace FundingMonitor.HistoricalRates.Contracts;

public sealed record HistoricalRatesQuery(
    Symbol Symbol,
    IReadOnlyCollection<ExchangeId>? Exchanges,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int? Limit);
