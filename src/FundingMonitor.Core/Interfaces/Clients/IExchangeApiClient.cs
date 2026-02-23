using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Clients;

public interface IExchangeApiClient
{
    ExchangeType ExchangeType { get; }
    Task<List<CurrentFundingRate>> GetCurrentFundingRatesAsync(CancellationToken cancellationToken);

    Task<List<HistoricalFundingRate>> GetHistoricalFundingRatesAsync(
        string symbol,
        DateTime fromTime,
        DateTime toTime,
        int limit,
        CancellationToken cancellationToken);

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
}