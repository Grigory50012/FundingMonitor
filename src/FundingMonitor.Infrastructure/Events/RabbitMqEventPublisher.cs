using System.Text;
using System.Text.Json;
using FundingMonitor.Core.Events;
using FundingMonitor.Core.Interfaces.Events;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace FundingMonitor.Infrastructure.Events;

public class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly IChannel _channel;
    private readonly IConnection _connection;
    private readonly string _exchangeName = "funding_events";
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public RabbitMqEventPublisher(
        IConnection connection,
        IChannel channel,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _connection = connection;
        _channel = channel;
        _logger = logger;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_channel?.IsOpen == true) await _channel.CloseAsync();
            if (_connection?.IsOpen == true) await _connection.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при освобождении ресурсов RabbitMQ поставщика");
        }
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : FundingEvent
    {
        try
        {
            var routingKey = GetRoutingKey(@event);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event, JsonSerializerOptions.Default));

            var properties = new BasicProperties
            {
                Persistent = true,
                Type = typeof(T).Name,
                MessageId = @event.EventId,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _channel.BasicPublishAsync(
                _exchangeName,
                routingKey,
                false,
                properties,
                body,
                cancellationToken);

            _logger.LogDebug("Published {EventType} for {Exchange}:{Symbol}",
                typeof(T).Name, @event.Exchange, @event.NormalizedSymbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось опубликовать событие {EventType}", typeof(T).Name);
            throw;
        }
    }

    public async Task PublishBatchAsync<T>(IEnumerable<T> events, CancellationToken cancellationToken = default)
        where T : FundingEvent
    {
        foreach (var @event in events) await PublishAsync(@event, cancellationToken);
    }

    public static async Task<RabbitMqEventPublisher> CreateAsync(
        IConnectionFactory connectionFactory,
        ILogger<RabbitMqEventPublisher> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
            var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await channel.ExchangeDeclareAsync(
                "funding_events",
                "topic",
                true,
                false,
                cancellationToken: cancellationToken);

            logger.LogInformation("RabbitMQ exchange 'funding_events' declared");

            return new RabbitMqEventPublisher(connection, channel, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось создать RabbitMQ поставщика");
            throw;
        }
    }

    private string GetRoutingKey(FundingEvent @event)
    {
        var exchange = @event.Exchange.ToString().ToLowerInvariant();
        return @event switch
        {
            NewSymbolDetectedEvent => $"symbol.new.{exchange}",
            FundingTimeChangedEvent => $"funding.changed.{exchange}",
            SymbolRemovedEvent => $"symbol.removed.{exchange}",
            _ => $"event.{exchange}"
        };
    }
}