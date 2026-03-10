using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Services;

public interface ICurrentFundingRateCollector
{
    Task<IReadOnlyCollection<CurrentFundingRate>> CollectFundingRatesAsync(CancellationToken cancellationToken);
}