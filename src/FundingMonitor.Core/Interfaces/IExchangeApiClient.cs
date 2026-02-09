using FundingMonitor.Core.Enums;
using FundingMonitor.Core.Models;

namespace FundingMonitor.Core.Interfaces;

public interface IExchangeApiClient
{
    ExchangeType ExchangeType { get; }
    
    Task<List<NormalizedFundingRate>> GetAllFundingRatesAsync(CancellationToken cancellationToken);
    Task<bool> IsAvailableAsync();
    
    bool IsRateLimited { get; }
}