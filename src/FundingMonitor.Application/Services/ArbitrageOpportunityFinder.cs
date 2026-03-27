using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Application.Services;

public class ArbitrageOpportunityFinder : IArbitrageOpportunityFinder
{
    private readonly ILogger<ArbitrageOpportunityFinder> _logger;

    public ArbitrageOpportunityFinder(ILogger<ArbitrageOpportunityFinder> logger)
    {
        _logger = logger;
    }

    public List<ArbitrageOpportunity> FindOpportunities(List<CurrentFundingRate> rates)
    {
        var groupedRates = rates
            .GroupBy(r => r.NormalizedSymbol)
            .Where(g => g.Count() >= 2) // Нужно минимум 2 биржи
            .ToList();

        var opportunities = new List<ArbitrageOpportunity>();

        foreach (var group in groupedRates)
        {
            var rate = group.ToList();
            var minRate = rate.Min(r => r.FundingRate);
            var maxRate = rate.Max(r => r.FundingRate);
            var difference = maxRate - minRate;

            if (difference > 0.0001m) // Минимальная разница 0.01%
            {
                opportunities.Add(new ArbitrageOpportunity
                {
                    Symbol = group.Key,
                    Rates = rate
                });
            }
        }

        _logger.LogInformation("Found {Count} arbitrage opportunities", opportunities.Count);
        return opportunities.OrderByDescending(o => o.MaxDifference).ToList();
    }
}