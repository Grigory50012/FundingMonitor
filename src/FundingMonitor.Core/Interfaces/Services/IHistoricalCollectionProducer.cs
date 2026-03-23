using FundingMonitor.Core.Events;

namespace FundingMonitor.Core.Interfaces.Services;

public interface IHistoricalCollectionProducer
{
    Task EnqueueHistoricalCollectionTasksAsync(List<FundingRateChangedEvent> events,
        CancellationToken cancellationToken);
}