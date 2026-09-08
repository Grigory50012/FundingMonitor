using FundingMonitor.Arbitrage.Contracts;
using FundingMonitor.Arbitrage.Domain;

namespace FundingMonitor.Arbitrage.Application;

public sealed class FundingArbitrageService
{
    private FundingArbitrageOpportunity[] _snapshot = [];

    public void ReplaceSnapshot(IEnumerable<FundingArbitrageOpportunity> opportunities)
    {
        var replacement = opportunities.ToArray();
        Volatile.Write(ref _snapshot, replacement);
    }

    public IReadOnlyList<FundingArbitrageOpportunity> Query(ArbitrageQuery query)
    {
        var snapshot = Volatile.Read(ref _snapshot);
        IEnumerable<FundingArbitrageOpportunity> filtered = snapshot;

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
