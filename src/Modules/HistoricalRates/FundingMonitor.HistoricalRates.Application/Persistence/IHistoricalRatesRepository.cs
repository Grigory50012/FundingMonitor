using FundingMonitor.HistoricalRates.Contracts;
using FundingMonitor.HistoricalRates.Domain;

namespace FundingMonitor.HistoricalRates.Application.Persistence;

public interface IHistoricalRatesRepository
{
    Task<IReadOnlyList<HistoricalFundingRate>> GetHistoryAsync(
        HistoricalRatesQuery query,
        CancellationToken cancellationToken);
}
