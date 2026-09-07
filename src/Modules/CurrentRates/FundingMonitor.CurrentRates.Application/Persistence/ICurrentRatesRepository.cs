using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates.Domain;

namespace FundingMonitor.CurrentRates.Application.Persistence;

public interface ICurrentRatesRepository
{
    Task<IReadOnlyList<CurrentFundingRate>> GetRatesAsync(
        CurrentRatesQuery query,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CurrentFundingRate>> GetAllRatesAsync(
        CancellationToken cancellationToken);
}
