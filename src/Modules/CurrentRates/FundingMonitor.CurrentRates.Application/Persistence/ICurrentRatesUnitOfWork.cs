using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates.Domain;
using FundingMonitor.SharedKernel;

namespace FundingMonitor.CurrentRates.Application.Persistence;

public interface ICurrentRatesUnitOfWork
{
    Task StoreSnapshotAsync(
        ExchangeId exchange,
        IReadOnlyCollection<CurrentFundingRate> rates,
        IReadOnlyCollection<FundingRateChangedIntegrationEvent> events,
        CancellationToken cancellationToken);
}
