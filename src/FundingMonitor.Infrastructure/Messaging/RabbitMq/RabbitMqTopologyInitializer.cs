using RabbitMQ.Client;

namespace FundingMonitor.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqTopologyInitializer
{
    private static readonly IDictionary<string, object?> QueueArguments =
        new Dictionary<string, object?>
        {
            ["x-queue-type"] = "classic"
        };

    private readonly RabbitMqConnection _connection;
    private readonly RabbitMqHealthState _healthState;

    public RabbitMqTopologyInitializer(
        RabbitMqConnection connection,
        RabbitMqHealthState healthState)
    {
        _connection = connection;
        _healthState = healthState;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var connection = await _connection.GetAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            RabbitMqTopology.ExchangeName,
            RabbitMQ.Client.ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        foreach (var exchange in Enum.GetValues<FundingMonitor.Core.Entities.ExchangeType>())
        {
            var queue = RabbitMqTopology.GetQueueName(exchange);
            var routingKey = RabbitMqTopology.GetRoutingKey(exchange);
            await DeclareAndBindAsync(channel, queue, routingKey, cancellationToken);
        }

        await DeclareAndBindAsync(
            channel,
            RabbitMqTopology.DeadQueueName,
            RabbitMqTopology.DeadRoutingKey,
            cancellationToken);
        _healthState.MarkHealthy();
    }

    private static async Task DeclareAndBindAsync(
        IChannel channel,
        string queue,
        string routingKey,
        CancellationToken cancellationToken)
    {
        await channel.QueueDeclareAsync(
            queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: QueueArguments,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queue,
            RabbitMqTopology.ExchangeName,
            routingKey,
            arguments: null,
            cancellationToken: cancellationToken);
    }
}
