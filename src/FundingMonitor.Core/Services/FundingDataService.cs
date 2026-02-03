using FundingMonitor.Core.Enums;
using FundingMonitor.Core.Interfaces;
using FundingMonitor.Core.Models;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Core.Services;

public class FundingDataService : IFundingDataService
{
    private readonly IEnumerable<IExchangeApiClient> _exchangeClients;
    private readonly ILogger<FundingDataService> _logger;
    
    public FundingDataService(
        IEnumerable<IExchangeApiClient> exchangeClients,
        ILogger<FundingDataService> logger)
    {
        _exchangeClients = exchangeClients;
        _logger = logger;
    }
    
    public async Task<List<NormalizedFundingRate>> CollectAllRatesAsync()
    {
        var allRates = new List<NormalizedFundingRate>();
        
        foreach (var client in _exchangeClients)
        {
            try
            {
                if (client.IsRateLimited)
                {
                    _logger.LogWarning("[{Exchange}] Skipped due to rate limit", client.ExchangeType);
                    continue;
                }
                
                var rates = await client.GetAllFundingRatesAsync();
                allRates.AddRange(rates);
                
                _logger.LogInformation("[{Exchange}] Collected {Count} rates", 
                    client.ExchangeType, rates.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Exchange}] Failed to collect rates", client.ExchangeType);
            }
        }
        
        return allRates;
    }
    
    public List<ArbitrageOpportunity> FindArbitrageOpportunitiesAsync(List<NormalizedFundingRate> processinRates)
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
    
    public async Task<Dictionary<ExchangeType, bool>> CheckExchangesStatusAsync()
    {
        var status = new Dictionary<ExchangeType, bool>();
        var tasks = _exchangeClients.ToDictionary(
            client => client.ExchangeType,
            client => client.IsAvailableAsync());
        
        await Task.WhenAll(tasks.Values);
        
        foreach (var (exchangeType, task) in tasks)
        {
            status[exchangeType] = await task;
        }
        
        return status;
    }
}