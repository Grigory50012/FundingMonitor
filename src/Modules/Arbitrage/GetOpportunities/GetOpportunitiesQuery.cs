using FundingMonitor.SharedKernel;

namespace FundingMonitor.Arbitrage.GetOpportunities;

internal sealed record GetOpportunitiesQuery(
    string? Symbol,
    IReadOnlyCollection<ExchangeId>? Exchanges,
    decimal? MinAprPercent = null,
    decimal? MaxAdversePriceSpreadPercent = null,
    bool SameFundingIntervalOnly = false);
