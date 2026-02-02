using FundingMonitor.Core.Models;

namespace FundingMonitor.Core.Interfaces;

public interface IFundingDataService
{
    Task<List<FundingRateComparison>> CompareFundingRatesAsync(List<string>? symbols = null);
    Task UpdateDatabaseFromExchangesAsync();
    Task<List<TradingPairInfo>> GetAvailablePairsFromExchangeAsync(string exchangeName);
}