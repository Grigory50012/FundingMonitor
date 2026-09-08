using FundingMonitor.SharedKernel;

namespace FundingMonitor.HistoricalRates.Domain;

public sealed record HistoricalFundingRate(
    ExchangeId Exchange,
    Symbol Symbol,
    decimal FundingRate,
    DateTimeOffset FundingTime,
    DateTimeOffset CollectedOnUtc);
