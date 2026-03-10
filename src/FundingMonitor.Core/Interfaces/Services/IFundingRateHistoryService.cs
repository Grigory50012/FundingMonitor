using FundingMonitor.Core.Events;

namespace FundingMonitor.Core.Interfaces.Services;

public interface IFundingRateHistoryService
{
    Task EnqueueHistoricalCollectionTasksAsync(List<FundingRateEvent> events, CancellationToken cancellationToken);
}