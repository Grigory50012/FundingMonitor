using FundingMonitor.HistoricalRates.Domain;

namespace FundingMonitor.HistoricalRates.Application.Persistence;

public interface IHistoricalRatesUnitOfWork
{
    Task StoreHistoryAsync(
        IReadOnlyCollection<HistoricalFundingRate> rates,
        CancellationToken cancellationToken);
}
