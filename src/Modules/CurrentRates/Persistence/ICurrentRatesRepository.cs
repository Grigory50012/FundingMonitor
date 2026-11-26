using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.SharedKernel;

namespace FundingMonitor.CurrentRates.Persistence;

internal interface ICurrentRatesRepository
{
    Task<IReadOnlyList<CurrentFundingRate>> GetRatesAsync(
        CurrentRatesQuery query,
        CancellationToken cancellationToken);

    Task<int> StoreSnapshotAsync(
        ExchangeId exchange,
        IReadOnlyCollection<CurrentFundingRate> rates,
        CancellationToken cancellationToken);
}
