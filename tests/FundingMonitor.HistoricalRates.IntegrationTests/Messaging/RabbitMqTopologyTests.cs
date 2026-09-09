using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.HistoricalRates.Infrastructure.Messaging;
using FundingMonitor.Messaging;
using FundingMonitor.Messaging.Abstractions;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;

namespace FundingMonitor.HistoricalRates.IntegrationTests.Messaging;

public sealed class RabbitMqTopologyTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4.2-management")
        .WithUsername("guest").WithPassword("guest").Build();
    private RabbitMqConnection _connection = null!;
    private RabbitMqMessagePublisher _publisher = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync(TestContext.Current.CancellationToken);
        var options = Options.Create(new RabbitMqOptions
        {
            Host = _container.Hostname,
            Port = _container.GetMappedPublicPort(5672),
            Username = "guest",
            Password = "guest"
        });
        _connection = new RabbitMqConnection(options);
        _publisher = new RabbitMqMessagePublisher(_connection, options);
        await new RabbitMqTopologyInitializer(_connection).InitializeAsync(
            TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_publisher is not null) await _publisher.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task InitializeAsync_DeclaresAllDurableQueues()
    {
        var connection = await _connection.GetAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        foreach (var queue in RabbitMqTopology.AllQueues)
            Assert.Equal(queue, (await channel.QueueDeclarePassiveAsync(
                queue, TestContext.Current.CancellationToken)).QueueName);
    }

    [Fact]
    public Task PublishAsync_BinanceRoutingKey_ReachesOnlyBinanceQueue() =>
        AssertRoutingAsync("Binance", "historical-rates.collect.binance");

    [Fact]
    public Task PublishAsync_BybitRoutingKey_ReachesOnlyBybitQueue() =>
        AssertRoutingAsync("Bybit", "historical-rates.collect.bybit");

    [Fact]
    public Task PublishAsync_OkxRoutingKey_ReachesOnlyOkxQueue() =>
        AssertRoutingAsync("OKX", "historical-rates.collect.okx");

    private async Task AssertRoutingAsync(
        string exchangeName,
        string expectedQueue)
    {
        await DrainAsync();
        var integrationEvent = CreateEvent(exchangeName);
        var result = await _publisher.PublishAsync(
            RabbitMqTopology.ExchangeName,
            RabbitMqTopology.GetRoutingKey(integrationEvent.Exchange),
            CreateEnvelope(integrationEvent),
            null,
            TestContext.Current.CancellationToken);
        Assert.Equal(MessagePublishResult.Success, result);

        var connection = await _connection.GetAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        foreach (var queue in RabbitMqTopology.CollectionQueues)
        {
            var message = await channel.BasicGetAsync(
                queue, true, TestContext.Current.CancellationToken);
            Assert.Equal(queue == expectedQueue, message is not null);
        }
    }

    [Fact]
    public async Task PublishAsync_Message_IsPersistent()
    {
        await DrainAsync();
        var integrationEvent = CreateEvent("Binance");
        await _publisher.PublishAsync(
            RabbitMqTopology.ExchangeName,
            RabbitMqTopology.GetRoutingKey(integrationEvent.Exchange),
            CreateEnvelope(integrationEvent), null, TestContext.Current.CancellationToken);
        var connection = await _connection.GetAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var message = await channel.BasicGetAsync(
            RabbitMqTopology.GetQueueName(integrationEvent.Exchange),
            true,
            TestContext.Current.CancellationToken);
        Assert.NotNull(message);
        Assert.True(message.BasicProperties.Persistent);
        var restored = MessagingJson.Deserialize<MessageEnvelope<FundingRateChangedIntegrationEvent>>(
            message.Body.Span);
        Assert.Equal(integrationEvent.Exchange, restored?.Payload.Exchange);
        Assert.Equal(integrationEvent.Symbol, restored?.Payload.Symbol);
    }

    [Fact]
    public async Task PublishAsync_UnknownRoutingKey_ReturnsUnroutable()
    {
        var integrationEvent = CreateEvent("Bybit");
        var result = await _publisher.PublishAsync(
            RabbitMqTopology.ExchangeName,
            "funding-rate.changed.unknown",
            CreateEnvelope(integrationEvent), null, TestContext.Current.CancellationToken);
        Assert.True(result.Confirmed);
        Assert.True(result.Returned);
    }

    private async Task DrainAsync()
    {
        var connection = await _connection.GetAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        foreach (var queue in RabbitMqTopology.AllQueues)
            await channel.QueuePurgeAsync(queue, TestContext.Current.CancellationToken);
    }

    private static FundingRateChangedIntegrationEvent CreateEvent(string exchange) => new(
        Guid.NewGuid(), DateTimeOffset.UtcNow, ExchangeId.Parse(exchange),
        Symbol.Create("BTC-USDT"), DateTimeOffset.UtcNow.AddHours(8));

    private static MessageEnvelope<FundingRateChangedIntegrationEvent> CreateEnvelope(
        FundingRateChangedIntegrationEvent integrationEvent) => new(
        integrationEvent.EventId, integrationEvent.EventId, null,
        integrationEvent.OccurredOnUtc, "funding-rate.changed", 1, integrationEvent);
}
