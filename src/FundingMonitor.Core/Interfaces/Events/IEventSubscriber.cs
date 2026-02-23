using FundingMonitor.Core.Events;

namespace FundingMonitor.Core.Interfaces.Events;

public interface IEventSubscriber<T> where T : FundingEvent
{
    string SubscriptionName { get; }
    Task HandleAsync(T @event, CancellationToken cancellationToken);
}