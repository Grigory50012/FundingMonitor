using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.HistoricalRates.Infrastructure.Messaging;
using FundingMonitor.Messaging;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Options;
using Testcontainers.RabbitMq;
using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.Messaging;

public sealed class RabbitMqTopologyTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _container =
        new RabbitMqBuilder("rabbitmq:4.2-management")
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();

    private RabbitMqConnection _connection = null!;
    private RabbitMqMessagePublisher _publisher = null!;

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _container.StartAsync(cancellationToken);
        var options = Options.Create(new RabbitMqOptions
        {
            Host = _container.Hostname,
            Port = _container.GetMappedPublicPort(5672),
            Username = "guest",
            Password = "guest",
            PublisherConfirmTimeoutSeconds = 10
        });
        var health = new RabbitMqTopologyHealthState();
        _connection = new RabbitMqConnection(options);
        var initializer = new RabbitMqTopologyInitializer(_connection, health);
        _publisher = new RabbitMqMessagePublisher(_connection, options);

        await initializer.InitializeAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_publisher is not null)
            await _publisher.DisposeAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task InitializeAsync_DeclaresAllDurableQueues()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var connection = await _connection.GetAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken);

        foreach (var queue in new[]
                 {
                     "historical-rates.collect.binance",
                     "historical-rates.collect.bybit",
                     "historical-rates.collect.okx",
                     "historical-rates.dead"
                 })
        {
            var declaration = await channel.QueueDeclarePassiveAsync(
                queue,
                cancellationToken);
            Assert.Equal(queue, declaration.QueueName);
        }
    }

    [Fact]
    public async Task PublishAsync_KnownRoutingKey_ConfirmsPersistentMessage()
    {
        var task = CreateMessage("Binance", "BTC-USDT");

        var result = await _publisher.PublishAsync(
            RabbitMqTopology.ExchangeName,
            "funding-rate.changed.binance",
            task,
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal(MessagePublishResult.Success, result);
        var cancellationToken = TestContext.Current.CancellationToken;
        var connection = await _connection.GetAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken);
        var message = await channel.BasicGetAsync(
            "historical-rates.collect.binance",
            true,
            cancellationToken);
        Assert.NotNull(message);
        Assert.True(message.BasicProperties.Persistent);
        Assert.Equal(task.MessageId.ToString(), message.BasicProperties.MessageId);
        Assert.Equal(
            task,
            MessagingJson.Deserialize<MessageEnvelope<FundingRateChangedIntegrationEvent>>(message.Body.Span));
    }

    [Fact]
    public async Task PublishAsync_UnknownRoutingKey_ReturnsUnroutable()
    {
        var task = CreateMessage("Bybit", "ETH-USDT");

        var result = await _publisher.PublishAsync(
            RabbitMqTopology.ExchangeName,
            "unknown",
            task,
            null,
            TestContext.Current.CancellationToken);

        Assert.True(result.Returned);
        Assert.True(result.Confirmed);
    }

    private static MessageEnvelope<FundingRateChangedIntegrationEvent> CreateMessage(string exchange, string symbol)
    {
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        return new MessageEnvelope<FundingRateChangedIntegrationEvent>(
            id, id, null, now, nameof(FundingRateChangedIntegrationEvent), 1,
            new FundingRateChangedIntegrationEvent(id, now, ExchangeId.Parse(exchange), Symbol.Create(symbol), now));
    }
}
