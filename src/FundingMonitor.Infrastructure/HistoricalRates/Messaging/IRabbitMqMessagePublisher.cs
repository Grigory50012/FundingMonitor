
namespace FundingMonitor.Infrastructure.HistoricalRates.Messaging;

public interface IRabbitMqMessagePublisher
{
    Task<RabbitMqPublishResult> PublishAsync(
        HistoricalCollectionTask task,
        string routingKey,
        IReadOnlyDictionary<string, object?>? headers,
        CancellationToken cancellationToken);
}
