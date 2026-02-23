using System.Text;
using System.Text.Json;
using FundingMonitor.Core.Events;
using FundingMonitor.Core.Interfaces.Events;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FundingMonitor.Infrastructure.Events;

public class RabbitMqEventConsumer<T> : IAsyncDisposable where T : FundingEvent
{
    private readonly IChannel _channel;
    private readonly IConnection _connection;
    private readonly string _exchangeName = "funding_events";
    private readonly ILogger _logger;
    private readonly string _queueName;
    private readonly IEventSubscriber<T> _subscriber;
    private string _consumerTag;

    public RabbitMqEventConsumer(
        IConnection connection,
        IChannel channel,
        IEventSubscriber<T> subscriber,
        ILogger logger)
    {
        _connection = connection;
        _channel = channel;
        _subscriber = subscriber;
        _logger = logger;
        _queueName = $"{subscriber.SubscriptionName}_{typeof(T).Name}";
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_consumerTag))
                await _channel.BasicCancelAsync(_consumerTag);

            if (_channel?.IsOpen == true) await _channel.CloseAsync();
            if (_connection?.IsOpen == true) await _connection.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при освобождении ресурсов RabbitMQ потребителя");
        }
    }

    public static async Task<RabbitMqEventConsumer<T>> CreateAsync(
        IConnectionFactory connectionFactory,
        IEventSubscriber<T> subscriber,
        ILogger logger,
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

            var queueName = $"{subscriber.SubscriptionName}_{typeof(T).Name}";

            await channel.QueueDeclareAsync(
                queueName,
                true,
                false,
                false,
                cancellationToken: cancellationToken);

            var routingKey = GetRoutingKeyPattern<T>();
            await channel.QueueBindAsync(
                queueName,
                "funding_events",
                routingKey,
                cancellationToken: cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            var instance = new RabbitMqEventConsumer<T>(connection, channel, subscriber, logger);

            consumer.ReceivedAsync += instance.OnMessageReceivedAsync;

            instance._consumerTag = await channel.BasicConsumeAsync(
                queueName,
                false,
                consumer,
                cancellationToken);

            logger.LogInformation("RabbitMQ потребитель запущен для {QueueName}", queueName);

            return instance;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось создать RabbitMQ потребителя для {EventType}", typeof(T).Name);
            throw;
        }
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        try
        {
            var body = args.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            var @event = JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Default);

            if (@event != null)
            {
                _logger.LogDebug("Обработка {EventId} для {Exchange}:{Symbol}",
                    @event.EventId, @event.Exchange, @event.NormalizedSymbol);

                await _subscriber.HandleAsync(@event, CancellationToken.None);
                await _channel.BasicAckAsync(args.DeliveryTag, false);

                _logger.LogDebug("Завершён: {EventId}", @event.EventId);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Ошибка десериализации JSON");
            await _channel.BasicNackAsync(args.DeliveryTag, false, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки события");
            await _channel.BasicNackAsync(args.DeliveryTag, false, true);
        }
    }

    private static string GetRoutingKeyPattern<TEvent>() where TEvent : FundingEvent
    {
        return typeof(TEvent).Name switch
        {
            nameof(NewSymbolDetectedEvent) => "symbol.new.*",
            nameof(FundingTimeChangedEvent) => "funding.changed.*",
            nameof(SymbolRemovedEvent) => "symbol.removed.*",
            _ => "event.*"
        };
    }
}