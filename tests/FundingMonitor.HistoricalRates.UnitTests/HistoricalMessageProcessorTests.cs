using System.Text.Json;
using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.HistoricalRates.Application;
using FundingMonitor.HistoricalRates.Infrastructure.Messaging;
using FundingMonitor.Messaging;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FundingMonitor.HistoricalRates.UnitTests;

public sealed class HistoricalMessageProcessorTests
{
    [Fact]
    public async Task ProcessAsync_TransientFailure_PublishesConfirmedRetryBeforeAck()
    {
        var publisher = new RecordingPublisher();
        var delay = new RecordingDelay();
        using var metrics = new HistoricalTransportMetrics();
        var processor = CreateProcessor(
            new ThrowingHandler(new TimeoutException("timeout")), publisher, delay, metrics);

        var result = await processor.ProcessAsync(Event, 0, TestContext.Current.CancellationToken);

        Assert.Equal(HistoricalMessageDisposition.Ack, result);
        var sent = Assert.Single(publisher.Messages);
        Assert.Equal("funding-rate.changed.binance", sent.RoutingKey);
        Assert.Equal(Event.EventId, sent.MessageId);
        Assert.Equal(1, sent.Headers["x-retry-count"]);
        Assert.Equal(1, delay.Calls);
    }

    [Fact]
    public async Task ProcessAsync_PermanentFailure_PublishesConfirmedDeadLetterBeforeAck()
    {
        var publisher = new RecordingPublisher();
        using var metrics = new HistoricalTransportMetrics();
        var processor = CreateProcessor(
            new ThrowingHandler(new JsonException("bad payload")), publisher,
            new RecordingDelay(), metrics);

        var result = await processor.ProcessAsync(Event, 0, TestContext.Current.CancellationToken);

        Assert.Equal(HistoricalMessageDisposition.Ack, result);
        Assert.Equal(RabbitMqTopology.DeadRoutingKey, Assert.Single(publisher.Messages).RoutingKey);
    }

    [Fact]
    public async Task ProcessAsync_RetryNotConfirmed_LeavesOriginalUnacked()
    {
        var publisher = new RecordingPublisher
        {
            Result = new MessagePublishResult(false, false, "nack")
        };
        using var metrics = new HistoricalTransportMetrics();
        var processor = CreateProcessor(
            new ThrowingHandler(new TimeoutException()), publisher,
            new RecordingDelay(), metrics);

        Assert.Equal(
            HistoricalMessageDisposition.LeaveUnacked,
            await processor.ProcessAsync(Event, 0, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ProcessMalformedAsync_ConfirmedDeadLetter_ReturnsAckAndPreservesOriginalBody()
    {
        var publisher = new RecordingPublisher();
        using var metrics = new HistoricalTransportMetrics();
        var processor = CreateProcessor(
            new ThrowingHandler(new InvalidOperationException()), publisher,
            new RecordingDelay(), metrics);
        var body = "{"u8.ToArray();

        var result = await processor.ProcessMalformedAsync(
            ExchangeId.Parse("Binance"),
            body,
            "not-a-guid",
            new JsonException("Invalid JSON"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HistoricalMessageDisposition.Ack, result);
        var sent = Assert.Single(publisher.Messages);
        Assert.Equal(RabbitMqTopology.DeadRoutingKey, sent.RoutingKey);
        var payload = Assert.IsType<MalformedHistoricalMessage>(sent.Payload);
        Assert.Equal(Convert.ToBase64String(body), payload.OriginalBodyBase64);
        Assert.Equal("not-a-guid", payload.OriginalMessageId);
    }

    [Fact]
    public async Task ProcessMalformedAsync_UnconfirmedDeadLetter_LeavesOriginalUnacked()
    {
        var publisher = new RecordingPublisher
        {
            Result = new MessagePublishResult(false, false, "nack")
        };
        using var metrics = new HistoricalTransportMetrics();
        var processor = CreateProcessor(
            new ThrowingHandler(new InvalidOperationException()), publisher,
            new RecordingDelay(), metrics);

        var result = await processor.ProcessMalformedAsync(
            ExchangeId.Parse("Binance"),
            "bad"u8.ToArray(),
            null,
            new JsonException("Invalid JSON"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HistoricalMessageDisposition.LeaveUnacked, result);
    }

    private static HistoricalMessageProcessor CreateProcessor(
        IHistoricalEventHandler handler,
        IMessagePublisher publisher,
        IHistoricalRetryDelay delay,
        HistoricalTransportMetrics metrics) => new(
        handler,
        new HistoricalCollectionFailureClassifier(),
        publisher,
        delay,
        Options.Create(new HistoricalDataCollectionOptions
        {
            MaxRetries = 3,
            RetryDelaySeconds = 1
        }),
        metrics,
        NullLogger<HistoricalMessageProcessor>.Instance);

    private static FundingRateChangedIntegrationEvent Event => new(
        Guid.Parse("40000000-0000-0000-0000-000000000001"),
        DateTimeOffset.UtcNow,
        ExchangeId.Parse("Binance"),
        Symbol.Create("BTC-USDT"),
        DateTimeOffset.UtcNow.AddHours(8));

    private sealed class ThrowingHandler(Exception exception) : IHistoricalEventHandler
    {
        public Task HandleAsync(
            FundingRateChangedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken) => Task.FromException(exception);
    }

    private sealed class RecordingDelay : IHistoricalRetryDelay
    {
        public int Calls { get; private set; }
        public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingPublisher : IMessagePublisher
    {
        public MessagePublishResult Result { get; init; } = MessagePublishResult.Success;
        public List<SentMessage> Messages { get; } = [];

        public Task<MessagePublishResult> PublishAsync<T>(
            string exchange,
            string routingKey,
            MessageEnvelope<T> message,
            IReadOnlyDictionary<string, object?>? headers,
            CancellationToken cancellationToken)
        {
            Messages.Add(new SentMessage(
                routingKey, message.MessageId, message.Payload!,
                headers ?? new Dictionary<string, object?>()));
            return Task.FromResult(Result);
        }
    }

    private sealed record SentMessage(
        string RoutingKey,
        Guid MessageId,
        object Payload,
        IReadOnlyDictionary<string, object?> Headers);
}
