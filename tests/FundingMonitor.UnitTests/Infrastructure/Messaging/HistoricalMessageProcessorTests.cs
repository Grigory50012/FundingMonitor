using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Failures;
using FundingMonitor.Core.Interfaces.Services;
using FundingMonitor.Core.Queues;
using FundingMonitor.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FundingMonitor.UnitTests.Infrastructure.Messaging;

public sealed class HistoricalMessageProcessorTests
{
    [Fact]
    public async Task ProcessAsync_CollectorSucceeds_ReturnsAck()
    {
        var fixture = new Fixture();

        var result = await fixture.ProcessAsync();

        Assert.Equal(HistoricalMessageDisposition.Ack, result);
        Assert.Empty(fixture.Publisher.Messages);
    }

    [Fact]
    public async Task ProcessAsync_TransientFailure_PublishesIncrementedRetryBeforeAck()
    {
        var fixture = new Fixture
        {
            CollectorException = new TimeoutException("exchange")
        };

        var result = await fixture.ProcessAsync();

        Assert.Equal(HistoricalMessageDisposition.Ack, result);
        var published = Assert.Single(fixture.Publisher.Messages);
        Assert.Equal("binance", published.RoutingKey);
        Assert.Equal(1, published.Task.RetryCount);
        Assert.Equal(fixture.Task.TaskId, published.Task.TaskId);
        Assert.Equal(1, fixture.Delay.CallCount);
    }

    [Fact]
    public async Task ProcessAsync_PermanentFailure_PublishesDeadLetterWithoutDelay()
    {
        var fixture = new Fixture
        {
            CollectorException = new InvalidDataException("payload"),
            Decision = HistoricalCollectionFailureDecision.DeadLetter
        };

        var result = await fixture.ProcessAsync();

        Assert.Equal(HistoricalMessageDisposition.Ack, result);
        var published = Assert.Single(fixture.Publisher.Messages);
        Assert.Equal(RabbitMqTopology.DeadRoutingKey, published.RoutingKey);
        Assert.Equal("Permanent", published.Headers["FailureCategory"]);
        Assert.Equal(0, fixture.Delay.CallCount);
    }

    [Fact]
    public async Task ProcessAsync_MaxRetriesReached_PublishesDeadLetter()
    {
        var fixture = new Fixture
        {
            Task = CreateTask(retryCount: 3),
            CollectorException = new TimeoutException("exchange")
        };

        var result = await fixture.ProcessAsync();

        Assert.Equal(HistoricalMessageDisposition.Ack, result);
        Assert.Equal(
            RabbitMqTopology.DeadRoutingKey,
            Assert.Single(fixture.Publisher.Messages).RoutingKey);
        Assert.Equal(0, fixture.Delay.CallCount);
    }

    [Fact]
    public async Task ProcessAsync_ShutdownCancellation_LeavesOriginalUnacked()
    {
        var fixture = new Fixture
        {
            CollectorException = new OperationCanceledException(),
            Decision = HistoricalCollectionFailureDecision.AbandonForShutdown
        };

        var result = await fixture.ProcessAsync();

        Assert.Equal(HistoricalMessageDisposition.LeaveUnacked, result);
        Assert.Empty(fixture.Publisher.Messages);
    }

    [Fact]
    public async Task ProcessAsync_RetryPublishNotConfirmed_LeavesOriginalUnacked()
    {
        var fixture = new Fixture
        {
            CollectorException = new TimeoutException("exchange")
        };
        fixture.Publisher.Result = RabbitMqPublishResult.NotConfirmed("connection");

        var result = await fixture.ProcessAsync();

        Assert.Equal(HistoricalMessageDisposition.LeaveUnacked, result);
    }

    private static HistoricalCollectionTask CreateTask(int retryCount = 0) =>
        new(
            Guid.NewGuid(),
            ExchangeType.Binance,
            "BTC-USDT",
            new DateTime(2026, 7, 26, 18, 0, 0, DateTimeKind.Utc),
            retryCount);

    private sealed class Fixture
    {
        public HistoricalCollectionTask Task { get; set; } = CreateTask();
        public Exception? CollectorException { get; set; }
        public HistoricalCollectionFailureDecision Decision { get; set; } =
            HistoricalCollectionFailureDecision.Retry;
        public RecordingPublisher Publisher { get; } = new();
        public RecordingDelay Delay { get; } = new();

        public Task<HistoricalMessageDisposition> ProcessAsync()
        {
            var processor = new HistoricalMessageProcessor(
                new StubCollector(() => CollectorException),
                new StubClassifier(() => Decision),
                Publisher,
                Delay,
                TimeProvider.System,
                Options.Create(new HistoricalDataCollectionOptions
                {
                    MaxRetries = 3,
                    RetryDelaySeconds = 30
                }),
                NullLogger<HistoricalMessageProcessor>.Instance);

            return processor.ProcessAsync(Task, CancellationToken.None);
        }
    }

    private sealed class StubCollector(Func<Exception?> exception)
        : IHistoricalFundingRateCollector
    {
        public Task CollectAndSaveAsync(
            HistoricalCollectionTask task,
            CancellationToken cancellationToken)
        {
            return exception() is { } value
                ? Task.FromException(value)
                : Task.CompletedTask;
        }
    }

    private sealed class StubClassifier(
        Func<HistoricalCollectionFailureDecision> decision)
        : IHistoricalCollectionFailureClassifier
    {
        public HistoricalCollectionFailureDecision Classify(
            Exception exception,
            CancellationToken applicationStoppingToken) => decision();
    }

    private sealed class RecordingPublisher : IRabbitMqMessagePublisher
    {
        public List<PublishedMessage> Messages { get; } = [];
        public RabbitMqPublishResult Result { get; set; } = RabbitMqPublishResult.Success;

        public Task<RabbitMqPublishResult> PublishAsync(
            HistoricalCollectionTask task,
            string routingKey,
            IReadOnlyDictionary<string, object?>? headers,
            CancellationToken cancellationToken)
        {
            Messages.Add(new PublishedMessage(
                task,
                routingKey,
                headers ?? new Dictionary<string, object?>()));
            return Task.FromResult(Result);
        }
    }

    private sealed class RecordingDelay : IHistoricalRetryDelay
    {
        public int CallCount { get; private set; }

        public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed record PublishedMessage(
        HistoricalCollectionTask Task,
        string RoutingKey,
        IReadOnlyDictionary<string, object?> Headers);
}
