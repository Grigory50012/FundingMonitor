using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Services;

namespace FundingMonitor.Application.Services.Arbitrage;

public class FundingArbitrageService : IFundingArbitrageService
{
    private volatile List<FundingArbitrageOpportunity> _opportunities = [];

    public void UpdateOpportunities(IEnumerable<FundingArbitrageOpportunity> opportunities)
    {
        _opportunities = opportunities.ToList();
    }

    public IReadOnlyList<FundingArbitrageOpportunity> GetSortedByAprDiff(string? symbol = null,
        List<ExchangeType>? exchanges = null)
    {
        var query = _opportunities.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(symbol))
            query = query.Where(o => o.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));

        if (exchanges is { Count: > 0 })
        {
            if (exchanges.Count < 2)
                return new List<FundingArbitrageOpportunity>().AsReadOnly();

            query = query.Where(o => exchanges.Contains(o.ExchangeA) && exchanges.Contains(o.ExchangeB));
        }

        return query
            .OrderByDescending(o => o.ProfitabilityPercent)
            .ToList()
            .AsReadOnly();
    }

    private static string NormalizeSymbol(string symbol)
    {
        return symbol.Contains('-')
            ? symbol
            : $"{symbol}-USDT";
    }
}