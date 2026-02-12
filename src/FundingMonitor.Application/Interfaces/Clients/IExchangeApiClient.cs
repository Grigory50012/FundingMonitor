using FundingMonitor.Core.Entities;

namespace FundingMonitor.Application.Interfaces.Clients;

public interface IExchangeApiClient
{
    ExchangeType ExchangeType { get; }
    Task<List<NormalizedFundingRate>> GetAllFundingRatesAsync(CancellationToken cancellationToken);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
}