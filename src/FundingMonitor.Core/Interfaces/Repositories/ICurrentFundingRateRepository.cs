using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Repositories;

public interface ICurrentFundingRateRepository
{
    Task UpdateRatesAsync(IEnumerable<CurrentFundingRate> rates, CancellationToken cancellationToken);

    Task<IEnumerable<CurrentFundingRate>> GetRatesAsync(string? symbol, List<ExchangeType>? exchanges,
        CancellationToken cancellationToken);
}