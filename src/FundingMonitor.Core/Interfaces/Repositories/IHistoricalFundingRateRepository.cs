using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Repositories;

public interface IHistoricalFundingRateRepository
{
    Task AddRangeAsync(IEnumerable<HistoricalFundingRate> rates, CancellationToken cancellationToken);

    Task<List<HistoricalFundingRate>> GetHistoryAsync(
        string symbol,
        List<ExchangeType>? exchanges,
        DateTime? from,
        DateTime? to,
        int? limit,
        CancellationToken cancellationToken);
}