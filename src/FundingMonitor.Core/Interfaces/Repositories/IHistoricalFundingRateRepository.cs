using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Repositories;

public interface IHistoricalFundingRateRepository
{
    Task SaveRatesAsync(IEnumerable<HistoricalFundingRate> rates, CancellationToken cancellationToken);

    Task<HistoricalFundingRate?> GetLastRateAsync(string exchange, string normalizedSymbol,
        CancellationToken cancellationToken);

    Task<bool> HasHistoryAsync(string exchange, string normalizedSymbol, CancellationToken cancellationToken);
}