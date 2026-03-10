using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Services;

public interface ICurrentDataCollector
{
    Task<IReadOnlyCollection<CurrentFundingRate>> CollectCurrentRatesAsync(CancellationToken cancellationToken);
}