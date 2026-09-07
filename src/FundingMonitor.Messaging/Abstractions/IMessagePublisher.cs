namespace FundingMonitor.Messaging;

public interface IMessagePublisher
{
    Task<MessagePublishResult> PublishAsync<T>(
        string exchange,
        string routingKey,
        MessageEnvelope<T> message,
        IReadOnlyDictionary<string, object?>? headers,
        CancellationToken cancellationToken);
}