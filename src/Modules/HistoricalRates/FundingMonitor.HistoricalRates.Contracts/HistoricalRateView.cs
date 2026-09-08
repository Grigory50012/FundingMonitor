using FundingMonitor.SharedKernel;

namespace FundingMonitor.HistoricalRates.Contracts;

public sealed record HistoricalRateView(
    ExchangeId Exchange,
    Symbol Symbol,
    decimal FundingRate,
    DateTimeOffset FundingTime,
    DateTimeOffset CollectedOnUtc);
