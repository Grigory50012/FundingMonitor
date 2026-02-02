using FundingMonitor.Core.Models;

namespace FundingMonitor.Core.Interfaces;

public interface IExchangeApiClient
{
    string ExchangeName { get; }
    
    // Получить все доступные пары
    Task<List<TradingPairInfo>> GetAvailablePairsAsync();
    
    // Получить текущую ставку финансирования для пары
    Task<FundingRateInfo> GetCurrentFundingRateAsync(string symbol);
    
    // Получить историю ставок финансирования
    Task<List<FundingRateInfo>> GetFundingRateHistoryAsync(string symbol, int limit = 100);
    
    // Получить предсказанную ставку финансирования
    Task<decimal?> GetPredictedFundingRateAsync(string symbol);
}