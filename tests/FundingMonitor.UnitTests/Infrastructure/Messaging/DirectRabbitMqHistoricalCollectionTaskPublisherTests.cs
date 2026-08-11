using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FundingMonitor.UnitTests.Infrastructure.Messaging;

public sealed class DirectRabbitMqHistoricalCollectionTaskPublisherTests
{
    [Fact]
    public async Task PublishAsync_DuplicateEvents_PublishesIndependentTasks()
    {
        var transport = new RecordingMessagePublisher();
        var publisher = CreatePublisher(transport);
        var events = new[]
        {
            CreateEvent(ExchangeType.Binance, "BTC-USDT"),
            CreateEvent(ExchangeType.Binance, "BTC-USDT")
        };

        await publisher.PublishAsync(events, CancellationToken.None);

        Assert.Equal(2, transport.Messages.Count);
        Assert.NotEqual(
            transport.Messages[0].Task.TaskId,
            transport.Messages[1].Task.TaskId);
        Assert.All(transport.Messages, message =>
        {
            Assert.Equal("binance", message.RoutingKey);
            Assert.Equal("BTC-USDT", message.Task.NormalizedSymbol);
            Assert.Equal(0, message.Task.RetryCount);
        });
    }

    [Theory]
    [InlineData(ExchangeType.Binance, "binance")]
    [InlineData(ExchangeType.Bybit, "bybit")]
    [InlineData(ExchangeType.OKX, "okx")]
    public async Task PublishAsync_Exchange_UsesExchangeRoutingKey(
        ExchangeType exchange,
        string expectedRoutingKey)
    {
        var transport = new RecordingMessagePublisher();
        var publisher = CreatePublisher(transport);

        await publisher.PublishAsync(
            [CreateEvent(exchange, "ETH-USDT")],
            CancellationToken.None);

        Assert.Equal(expectedRoutingKey, Assert.Single(transport.Messages).RoutingKey);
    }

    [Fact]
    public async Task PublishAsync_UnconfirmedMessage_Throws()
    {
        var transport = new RecordingMessagePublisher
        {
            Result = RabbitMqPublishResult.NotConfirmed("nack")
        };
        var publisher = CreatePublisher(transport);

        await Assert.ThrowsAsync<RabbitMqPublishException>(() =>
            publisher.PublishAsync(
                [CreateEvent(ExchangeType.Bybit, "SOL-USDT")],
                CancellationToken.None));
    }

    private static DirectRabbitMqHistoricalCollectionTaskPublisher CreatePublisher(
        IRabbitMqMessagePublisher transport)
    {
        return new DirectRabbitMqHistoricalCollectionTaskPublisher(
            transport,
            TimeProvider.System,
            NullLogger<DirectRabbitMqHistoricalCollectionTaskPublisher>.Instance);
    }

    private static FundingRateChangedEvent CreateEvent(
        ExchangeType exchange,
        string symbol)
    {
        return new FundingRateChangedEvent
        {
            Exchange = exchange,
            NormalizedSymbol = symbol
        };
    }

    private sealed class RecordingMessagePublisher : IRabbitMqMessagePublisher
    {
        public List<(HistoricalCollectionTask Task, string RoutingKey)> Messages { get; } = [];
        public RabbitMqPublishResult Result { get; init; } = RabbitMqPublishResult.Success;

        public Task<RabbitMqPublishResult> PublishAsync(
            HistoricalCollectionTask task,
            string routingKey,
            IReadOnlyDictionary<string, object?>? headers,
            CancellationToken cancellationToken)
        {
            Messages.Add((task, routingKey));
            return Task.FromResult(Result);
        }
    }
}
