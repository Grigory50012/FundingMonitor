using FundingMonitor.HistoricalRates.Domain;

namespace FundingMonitor.HistoricalRates.Application.Persistence;

public interface IHistoricalRatesUnitOfWork
{
    Task<bool> HasProcessedAsync(
        Guid eventId,
        string consumer,
        CancellationToken cancellationToken);

    Task StoreHistoryAndMarkProcessedAsync(
        Guid eventId,
        string consumer,
        IReadOnlyCollection<HistoricalFundingRate> rates,
        CancellationToken cancellationToken);
}
