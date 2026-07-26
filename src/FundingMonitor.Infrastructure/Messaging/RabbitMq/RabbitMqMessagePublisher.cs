using System.Text.Json;
using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Queues;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace FundingMonitor.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqMessagePublisher :
    IRabbitMqMessagePublisher,
    IAsyncDisposable
{
    private readonly RabbitMqConnection _connection;
    private readonly RabbitMqHealthState _healthState;
    private readonly TimeSpan _confirmTimeout;
    private readonly SemaphoreSlim _publishLock = new(1, 1);
    private IChannel? _channel;

    public RabbitMqMessagePublisher(
        RabbitMqConnection connection,
        RabbitMqHealthState healthState,
        IOptions<RabbitMqOptions> options)
    {
        _connection = connection;
        _healthState = healthState;
        _confirmTimeout = TimeSpan.FromSeconds(
            options.Value.PublisherConfirmTimeoutSeconds);
    }

    public async Task<RabbitMqPublishResult> PublishAsync(
        HistoricalCollectionTask task,
        string routingKey,
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
                MessageId = task.TaskId.ToString(),
                ContentType = "application/json",
                Timestamp = new AmqpTimestamp(
                    new DateTimeOffset(task.CreatedAt).ToUnixTimeSeconds()),
                Headers = headers is null
                    ? null
                    : headers.ToDictionary(pair => pair.Key, pair => pair.Value)
            };
            var body = JsonSerializer.SerializeToUtf8Bytes(task);

            using var timeoutSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(_confirmTimeout);

            try
            {
                await channel.BasicPublishAsync(
                    RabbitMqTopology.ExchangeName,
                    routingKey,
                    mandatory: true,
                    properties,
                    body,
                    timeoutSource.Token);
                _healthState.MarkHealthy();
                return RabbitMqPublishResult.Success;
            }
            catch (PublishException exception) when (exception.IsReturn)
            {
                _healthState.MarkDegraded(exception.Message);
                return RabbitMqPublishResult.Unroutable(exception.Message);
            }
            catch (PublishException exception)
            {
                _healthState.MarkDegraded(exception.Message);
                return RabbitMqPublishResult.NotConfirmed(exception.Message);
            }
            catch (OperationCanceledException exception)
                when (!cancellationToken.IsCancellationRequested)
            {
                _healthState.MarkDegraded("Publisher confirm timed out");
                return RabbitMqPublishResult.NotConfirmed(
                    $"Publisher confirm timed out: {exception.Message}");
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                await ResetChannelAsync();
                _healthState.MarkDegraded(exception.Message);
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

    private async Task<IChannel> GetChannelAsync(
        CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await ResetChannelAsync();
        var connection = await _connection.GetAsync(cancellationToken);
        _channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            cancellationToken);
        return _channel;
    }

    private async Task ResetChannelAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        _channel = null;
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
}
