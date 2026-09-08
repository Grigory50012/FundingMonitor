using FundingMonitor.Messaging;
using FundingMonitor.SharedKernel;
using RabbitMQ.Client;

namespace FundingMonitor.HistoricalRates.Infrastructure.Messaging;

internal sealed class RabbitMqTopologyInitializer(
    RabbitMqConnection connection,
    RabbitMqTopologyHealthState? healthState = null)
{
    private static readonly ExchangeId[] Exchanges =
    [
        ExchangeId.Parse("Binance"), ExchangeId.Parse("Bybit"), ExchangeId.Parse("OKX")
    ];

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var rabbitConnection = await connection.GetAsync(cancellationToken);
            await using var channel = await rabbitConnection.CreateChannelAsync(
                cancellationToken: cancellationToken);
            await channel.ExchangeDeclareAsync(
                RabbitMqTopology.ExchangeName,
                RabbitMQ.Client.ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);
            foreach (var exchange in Exchanges)
                await DeclareAndBindAsync(
                    channel,
                    RabbitMqTopology.GetQueueName(exchange),
                    RabbitMqTopology.GetRoutingKey(exchange),
                    cancellationToken);
            await DeclareAndBindAsync(
                channel,
                RabbitMqTopology.DeadQueueName,
                RabbitMqTopology.DeadRoutingKey,
                cancellationToken);
            healthState?.Healthy();
        }
        catch (Exception exception)
        {
            healthState?.Failed(exception);
            throw;
        }
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
            arguments: new Dictionary<string, object?> { ["x-queue-type"] = "classic" },
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queue,
            RabbitMqTopology.ExchangeName,
            routingKey,
            arguments: null,
            cancellationToken: cancellationToken);
    }
}
