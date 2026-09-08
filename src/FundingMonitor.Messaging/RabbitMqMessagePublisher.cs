using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace FundingMonitor.Messaging;

public sealed class RabbitMqMessagePublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly TimeSpan _confirmTimeout;
    private readonly RabbitMqConnection _connection;
    private readonly SemaphoreSlim _publishLock = new(1, 1);
    private IChannel? _channel;

    public RabbitMqMessagePublisher(
        RabbitMqConnection connection,
        IOptions<RabbitMqOptions> options)
    {
        _connection = connection;
        _confirmTimeout = TimeSpan.FromSeconds(
            options.Value.PublisherConfirmTimeoutSeconds);
    }

    public async ValueTask DisposeAsync()
    {
        await _publishLock.WaitAsync();
        try
        {
            await ResetChannelAsync();
        }
        finally
        {
            _publishLock.Release();
            _publishLock.Dispose();
        }
    }

    public async Task<MessagePublishResult> PublishAsync<T>(
        string exchange,
        string routingKey,
        MessageEnvelope<T> message,
        IReadOnlyDictionary<string, object?>? headers,
        CancellationToken cancellationToken)
    {
        await _publishLock.WaitAsync(cancellationToken);
        try
        {
            var channel = await GetChannelAsync(cancellationToken);
            var properties = new BasicProperties
            {
                Persistent = true,
                MessageId = message.MessageId.ToString(),
                CorrelationId = message.CorrelationId.ToString(),
                Type = message.MessageType,
                ContentType = "application/json",
                Timestamp = new AmqpTimestamp(message.OccurredOnUtc.ToUnixTimeSeconds()),
                Headers = headers is null
                    ? null
                    : headers.ToDictionary(pair => pair.Key, pair => pair.Value)
            };
            var body = MessagingJson.SerializeToUtf8Bytes(message);

            using var timeoutSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(_confirmTimeout);

            try
            {
                await channel.BasicPublishAsync(
                    exchange,
                    routingKey,
                    true,
                    properties,
                    body,
                    timeoutSource.Token);
                return MessagePublishResult.Success;
            }
            catch (PublishException exception) when (exception.IsReturn)
            {
                return new MessagePublishResult(true, true, exception.Message);
            }
            catch (PublishException exception)
            {
                return new MessagePublishResult(false, false, exception.Message);
            }
            catch (OperationCanceledException exception)
                when (!cancellationToken.IsCancellationRequested)
            {
                return new MessagePublishResult(
                    false,
                    false,
                    $"Publisher confirm timed out: {exception.Message}");
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                await ResetChannelAsync();
                throw new RabbitMqPublishException(
                    "RabbitMQ publication failed",
                    exception);
            }
        }
        finally
        {
            _publishLock.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await ResetChannelAsync();
        var connection = await _connection.GetAsync(cancellationToken);
        _channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                true,
                true),
            cancellationToken);
        return _channel;
    }

    private async Task ResetChannelAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        _channel = null;
    }
}
