using FundingMonitor.Core.Enums;
using FundingMonitor.Core.Models;

namespace FundingMonitor.Core.Interfaces;

public interface IExchangeApiClient
{
    ExchangeType ExchangeType { get; }
    
    Task<List<NormalizedFundingRate>> GetAllFundingRatesAsync();
    Task<NormalizedFundingRate?> GetFundingRateAsync(string symbol);
    Task<bool> IsAvailableAsync();
    
    // Статистика
    int RequestsMade { get; }
    bool IsRateLimited { get; }
}