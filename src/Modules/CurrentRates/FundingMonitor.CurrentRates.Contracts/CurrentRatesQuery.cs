using FundingMonitor.SharedKernel;

namespace FundingMonitor.CurrentRates.Contracts;

public sealed record CurrentRatesQuery(
    string? Symbol,
    IReadOnlyCollection<ExchangeId>? Exchanges);
