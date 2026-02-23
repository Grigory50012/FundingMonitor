using FundingMonitor.Core.Events;

namespace FundingMonitor.Core.Interfaces.Events;

public interface IEventBus
{
    Task SubscribeAsync<T>(IEventSubscriber<T> subscriber, CancellationToken cancellationToken = default)
        where T : FundingEvent;

    Task UnsubscribeAsync<T>(IEventSubscriber<T> subscriber, CancellationToken cancellationToken = default)
        where T : FundingEvent;
}