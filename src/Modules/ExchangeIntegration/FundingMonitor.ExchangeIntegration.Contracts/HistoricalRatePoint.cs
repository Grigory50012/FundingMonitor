using FundingMonitor.SharedKernel;

namespace FundingMonitor.ExchangeIntegration.Contracts;

public sealed record HistoricalRatePoint(
    ExchangeId Exchange,
    Symbol Symbol,
    decimal FundingRate,
    DateTimeOffset FundingTime,
    DateTimeOffset CollectedOnUtc);
