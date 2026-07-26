using FundingMonitor.Core.Events;

namespace FundingMonitor.Core.Interfaces.Services;

public interface IHistoricalCollectionTaskPublisher
{
    Task PublishAsync(
        IReadOnlyCollection<FundingRateChangedEvent> events,
        CancellationToken cancellationToken);
}
