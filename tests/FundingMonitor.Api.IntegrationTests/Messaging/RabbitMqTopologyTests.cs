using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;
using FundingExchangeType = FundingMonitor.Core.Shared.Exchanges.ExchangeType;

namespace FundingMonitor.Api.IntegrationTests.Messaging;

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
        var health = new RabbitMqHealthState();
        _connection = new RabbitMqConnection(options);
        var initializer = new RabbitMqTopologyInitializer(_connection, health);
        _publisher = new RabbitMqMessagePublisher(_connection, health, options);

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
                     "history.binance",
                     "history.bybit",
                     "history.okx",
                     "history.dead"
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
        var task = new HistoricalCollectionTask(
            Guid.NewGuid(),
            FundingExchangeType.Binance,
            "BTC-USDT",
            DateTime.UtcNow);

        var result = await _publisher.PublishAsync(
            task,
            "binance",
            headers: null,
            TestContext.Current.CancellationToken);

        Assert.Equal(RabbitMqPublishResult.Success, result);
        var cancellationToken = TestContext.Current.CancellationToken;
        var connection = await _connection.GetAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken);
        var message = await channel.BasicGetAsync(
            "history.binance",
            autoAck: true,
            cancellationToken);
        Assert.NotNull(message);
        Assert.True(message.BasicProperties.Persistent);
        Assert.Equal(task.TaskId.ToString(), message.BasicProperties.MessageId);
        Assert.Equal(
            task,
            JsonSerializer.Deserialize<HistoricalCollectionTask>(message.Body.Span));
    }

    [Fact]
    public async Task PublishAsync_UnknownRoutingKey_ReturnsUnroutable()
    {
        var task = new HistoricalCollectionTask(
            Guid.NewGuid(),
            FundingExchangeType.Bybit,
            "ETH-USDT",
            DateTime.UtcNow);

        var result = await _publisher.PublishAsync(
            task,
            "unknown",
            headers: null,
            TestContext.Current.CancellationToken);

        Assert.True(result.Returned);
        Assert.True(result.Confirmed);
    }
}
