using FundingMonitor.Core.Enums;
using FundingMonitor.Core.Models;

namespace FundingMonitor.Core.Interfaces;

public interface IFundingDataService
{
    Task<List<NormalizedFundingRate>> CollectAllRatesAsync(CancellationToken cancellationToken);
    List<ArbitrageOpportunity> FindArbitrageOpportunitiesAsync(List<NormalizedFundingRate> rates);
    Task<Dictionary<ExchangeType, bool>> CheckExchangesStatusAsync();
}