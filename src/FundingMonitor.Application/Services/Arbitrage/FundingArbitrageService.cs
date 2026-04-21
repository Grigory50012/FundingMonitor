using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Application.Services.Arbitrage;

public class FundingArbitrageService : IFundingArbitrageService
{
    private readonly ILogger<FundingArbitrageService> _logger;
    private volatile List<FundingArbitrageOpportunity> _opportunities = [];

    public FundingArbitrageService(ILogger<FundingArbitrageService> logger)
    {
        _logger = logger;
    }


    public void UpdateOpportunities(IEnumerable<FundingArbitrageOpportunity> opportunities)
    {
        _opportunities = opportunities.ToList();
        _logger.LogDebug("Updated: count={Count}", _opportunities.Count);
    }

    public IReadOnlyList<FundingArbitrageOpportunity> GetSortedByAprDiff(string? symbol = null,
        List<ExchangeType>? exchanges = null)
    {
        var query = _opportunities.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(symbol))
            query = query.Where(o => o.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));

        if (exchanges != null && exchanges.Count > 0)
        {
            // If only one exchange is selected, show no results
            if (exchanges.Count < 2) return new List<FundingArbitrageOpportunity>().AsReadOnly();

            // When two or more exchanges are selected, only arb opportunities where both sides
            // belong to the selected set should be shown.
            query = query.Where(o => exchanges.Contains(o.ExchangeA) && exchanges.Contains(o.ExchangeB));
        }

        return query
            .OrderByDescending(o => o.ProfitabilityPercent)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<FundingArbitrageOpportunity> GetBySymbol(string symbol)
    {
        return _opportunities
            .Where(o => o.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }
}