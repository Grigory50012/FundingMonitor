using FundingMonitor.Core.Queues;

namespace FundingMonitor.Infrastructure.Messaging.RabbitMq;

public interface IRabbitMqMessagePublisher
{
    Task<RabbitMqPublishResult> PublishAsync(
        HistoricalCollectionTask task,
        string routingKey,
        IReadOnlyDictionary<string, object?>? headers,
        CancellationToken cancellationToken);
}
