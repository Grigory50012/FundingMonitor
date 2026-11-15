using FundingMonitor.Arbitrage.Contracts;
using FundingMonitor.Arbitrage.Domain;

namespace FundingMonitor.Arbitrage.Application;

public sealed class FundingArbitrageService
{
    public IReadOnlyList<FundingArbitrageOpportunity> Query(
        IReadOnlyList<FundingArbitrageOpportunity> opportunities,
        ArbitrageQuery query)
    {
        IEnumerable<FundingArbitrageOpportunity> filtered = opportunities;

        if (!string.IsNullOrWhiteSpace(query.Symbol))
        {
            var requested = query.Symbol.Trim();
            filtered = filtered.Where(opportunity =>
                opportunity.Symbol.Value.Equals(requested, StringComparison.OrdinalIgnoreCase) ||
                BaseAsset(opportunity.Symbol.Value).Equals(requested, StringComparison.OrdinalIgnoreCase));
        }

        if (query.Exchanges is { Count: > 0 })
        {
            if (query.Exchanges.Count < 2)
                return new List<FundingArbitrageOpportunity>().AsReadOnly();

            var exchanges = query.Exchanges.ToHashSet();
            filtered = filtered.Where(opportunity =>
                exchanges.Contains(opportunity.ExchangeA) &&
                exchanges.Contains(opportunity.ExchangeB));
        }

        if (query.MinAprPercent is { } minApr)
            filtered = filtered.Where(opportunity => opportunity.AprSpread >= minApr);

        if (query.MaxAdversePriceSpreadPercent is { } maxSpread)
            filtered = filtered.Where(opportunity => opportunity.PriceSpreadPercent >= -maxSpread);

        if (query.SameFundingIntervalOnly)
            filtered = filtered.Where(opportunity =>
                opportunity.FundingIntervalHoursA is > 0 &&
                opportunity.FundingIntervalHoursA == opportunity.FundingIntervalHoursB);

        return filtered
            .OrderByDescending(opportunity => opportunity.AprSpread)
            .ToList()
            .AsReadOnly();
    }

    private static string BaseAsset(string symbol)
    {
        var separator = symbol.IndexOf('-');
        return separator < 0 ? symbol : symbol[..separator];
    }
}
