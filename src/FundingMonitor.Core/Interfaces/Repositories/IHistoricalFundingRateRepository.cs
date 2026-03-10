using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Repositories;

public interface IHistoricalFundingRateRepository
{
    Task SaveAsync(IEnumerable<HistoricalFundingRate> rates, CancellationToken cancellationToken);

    Task<HistoricalFundingRate?> GetLastAsync(string exchange, string normalizedSymbol,
        CancellationToken cancellationToken);
}