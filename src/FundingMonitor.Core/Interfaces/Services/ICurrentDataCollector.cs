using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Services;

public interface ICurrentDataCollector
{
    Task<List<CurrentFundingRate>> CollectAsync(CancellationToken cancellationToken);
}