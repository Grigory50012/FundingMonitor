using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.SharedKernel;

namespace FundingMonitor.CurrentRates.Persistence;

internal interface ICurrentRatesRepository
{
    Task<IReadOnlyList<CurrentFundingRate>> GetRatesAsync(
        CurrentRatesQuery query,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CurrentFundingRate>> GetAllRatesAsync(
        CancellationToken cancellationToken);

    Task StoreSnapshotAsync(
        ExchangeId exchange,
        IReadOnlyCollection<CurrentFundingRate> rates,
        IReadOnlyCollection<FundingRateChangedIntegrationEvent> events,
        CancellationToken cancellationToken);
}
