using FundingMonitor.SharedKernel;

namespace FundingMonitor.ExchangeIntegration.Contracts;

public sealed record HistoricalRatesRequest(
    ExchangeId Exchange,
    Symbol Symbol,
    DateTimeOffset From,
    DateTimeOffset To,
    int Limit);
