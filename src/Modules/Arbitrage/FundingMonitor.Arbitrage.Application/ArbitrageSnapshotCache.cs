using FundingMonitor.Arbitrage.Domain;
using FundingMonitor.CurrentRates.Contracts;

namespace FundingMonitor.Arbitrage.Application;

public sealed class ArbitrageSnapshotCache
{
    private readonly object gate = new();
    private CurrentRateView[]? previousRates;
    private IReadOnlyList<FundingArbitrageOpportunity> opportunities = [];

    public IReadOnlyList<FundingArbitrageOpportunity> GetOrCalculate(
        IReadOnlyList<CurrentRateView> rates,
        Func<IReadOnlyList<CurrentRateView>, IReadOnlyList<FundingArbitrageOpportunity>> calculate)
    {
        var ordered = rates.OrderBy(rate => rate.Symbol.Value, StringComparer.Ordinal)
            .ThenBy(rate => rate.Exchange.Value, StringComparer.Ordinal).ToArray();
        lock (gate)
        {
            if (previousRates is not null && previousRates.SequenceEqual(ordered))
                return opportunities;

            var result = calculate(ordered);
            previousRates = ordered;
            opportunities = result;
            return opportunities;
        }
    }
}
