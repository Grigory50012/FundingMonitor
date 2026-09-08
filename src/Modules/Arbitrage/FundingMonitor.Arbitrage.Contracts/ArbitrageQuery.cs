using FundingMonitor.SharedKernel;

namespace FundingMonitor.Arbitrage.Contracts;

public sealed record ArbitrageQuery(
    string? Symbol,
    IReadOnlyCollection<ExchangeId>? Exchanges);
