using FundingMonitor.Core.Events;

namespace FundingMonitor.Core.Interfaces.Events;

public interface IEventPublisher
{
    Task PublishBatchAsync<T>(IEnumerable<T> events, CancellationToken cancellationToken = default)
        where T : FundingEvent;
}