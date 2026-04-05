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

    public int Count => _opportunities.Count;
    public DateTime? LastUpdated { get; private set; }

    public void UpdateOpportunities(IEnumerable<FundingArbitrageOpportunity> opportunities)
    {
        _opportunities = opportunities.ToList();
        LastUpdated = DateTime.UtcNow;
        _logger.LogDebug("Updated: count={Count}", _opportunities.Count);
    }

    public IReadOnlyList<FundingArbitrageOpportunity> GetBySymbol(string symbol)
    {
        return _opportunities
            .Where(o => o.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<FundingArbitrageOpportunity> GetSortedByAprDiff()
    {
        return _opportunities
            .OrderByDescending(o => o.ProfitabilityPercent)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<FundingArbitrageOpportunity> GetAll()
    {
        return _opportunities.AsReadOnly();
    }
}