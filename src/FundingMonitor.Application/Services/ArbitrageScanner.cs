using FundingMonitor.Application.Interfaces.Services;
using FundingMonitor.Core.Entities;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Application.Services;

public class ArbitrageScanner : IArbitrageScanner
{
    private readonly ILogger<ArbitrageScanner> _logger;

    public ArbitrageScanner(ILogger<ArbitrageScanner> logger)
    {
        _logger = logger;
    }

    public List<ArbitrageOpportunity> FindOpportunities(List<NormalizedFundingRate> processinRates)
    {
        var groupedRates = processinRates
            .GroupBy(r => r.NormalizedSymbol)
            .Where(g => g.Count() >= 2) // Нужно минимум 2 биржи
            .ToList();
        
        var opportunities = new List<ArbitrageOpportunity>();
        
        foreach (var group in groupedRates)
        {
            var rates = group.ToList();
            var minRate = rates.Min(r => r.FundingRate);
            var maxRate = rates.Max(r => r.FundingRate);
            var difference = maxRate - minRate;
            
            if (difference > 0.0001m) // Минимальная разница 0.01%
            {
                opportunities.Add(new ArbitrageOpportunity
                {
                    Symbol = group.Key,
                    Rates = rates
                });
            }
        }
        
        _logger.LogInformation("Found {Count} arbitrage opportunities", opportunities.Count);
        return opportunities.OrderByDescending(o => o.MaxDifference).ToList();
    }
}