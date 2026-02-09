using FundingMonitor.Core.Entities;

namespace FundingMonitor.Application.Interfaces.Services;

public interface IFundingDataService
{
    Task<List<NormalizedFundingRate>> CollectAllRatesAsync(CancellationToken cancellationToken);
    List<ArbitrageOpportunity> FindArbitrageOpportunitiesAsync(List<NormalizedFundingRate> rates);
    Task<Dictionary<ExchangeType, bool>> CheckExchangesStatusAsync();
}