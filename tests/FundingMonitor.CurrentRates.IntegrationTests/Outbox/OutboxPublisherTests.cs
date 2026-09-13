using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates.Infrastructure.Outbox;
using FundingMonitor.CurrentRates.IntegrationTests.Infrastructure;
using FundingMonitor.Messaging;
using FundingMonitor.Messaging.Abstractions;
using FundingMonitor.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FundingMonitor.CurrentRates.IntegrationTests.Outbox;

[Collection("CurrentRatesPostgreSql")]
public sealed class OutboxPublisherTests(CurrentRatesPostgreSqlFixture fixture)
{
    [Fact]
    public async Task DatabaseCommitAndConfirmedPublish_MarksProcessed()
    {
        await fixture.ResetAsync();
        await fixture.SeedOutboxAsync(CreateMessage());
        var publisher = new StubPublisher(_ => MessagePublishResult.Success);
        await using var scope = await fixture.CreatePublisherScopeAsync(publisher, Now);

        await scope.Publisher.ProcessPendingAsync(TestContext.Current.CancellationToken);

        var message = await ReadSingleAsync();
        Assert.NotNull(message.ProcessedOnUtc);
        Assert.Equal(0, message.Attempts);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task UnconfirmedOrReturnedPublish_LeavesPending(bool confirmed, bool returned)
    {
        await fixture.ResetAsync();
        await fixture.SeedOutboxAsync(CreateMessage());
        var publisher = new StubPublisher(_ => new MessagePublishResult(confirmed, returned, "not delivered"));
        await using var scope = await fixture.CreatePublisherScopeAsync(publisher, Now);

        await scope.Publisher.ProcessPendingAsync(TestContext.Current.CancellationToken);

        var message = await ReadSingleAsync();
        Assert.Null(message.ProcessedOnUtc);
        Assert.Equal(1, message.Attempts);
        Assert.Equal("not delivered", message.Error);
    }

    [Fact]
    public async Task PublisherThrows_LeavesPendingAndIncrementsAttempts()
    {
        await fixture.ResetAsync();
        await fixture.SeedOutboxAsync(CreateMessage());
        var publisher = new StubPublisher(_ => throw new InvalidOperationException("RabbitMQ unavailable"));
        await using var scope = await fixture.CreatePublisherScopeAsync(publisher, Now);

        await scope.Publisher.ProcessPendingAsync(TestContext.Current.CancellationToken);

        var message = await ReadSingleAsync();
        Assert.Null(message.ProcessedOnUtc);
        Assert.Equal(1, message.Attempts);
        Assert.Contains("RabbitMQ unavailable", message.Error);
    }

    [Fact]
    public async Task FailedPublish_NewPublisherRetriesPendingRow()
    {
        await fixture.ResetAsync();
        await fixture.SeedOutboxAsync(CreateMessage());
        await using (var failed = await fixture.CreatePublisherScopeAsync(
            new StubPublisher(_ => new MessagePublishResult(false, false, "offline")), Now))
            await failed.Publisher.ProcessPendingAsync(TestContext.Current.CancellationToken);
        var publisher = new StubPublisher(_ => MessagePublishResult.Success);
        await using var scope = await fixture.CreatePublisherScopeAsync(publisher, Now);

        await scope.Publisher.ProcessPendingAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, publisher.Calls);
        var row = await ReadSingleAsync();
        Assert.NotNull(row.ProcessedOnUtc);
        Assert.Equal(1, row.Attempts);
        Assert.Null(row.Error);
        await scope.Publisher.ProcessPendingAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, publisher.Calls);
    }

    private static readonly DateTimeOffset Now =
        new(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

    private static OutboxMessage CreateMessage()
    {
        var integrationEvent = new FundingRateChangedIntegrationEvent(
            Guid.Parse("20000000-0000-0000-0000-000000000001"),
            Now.AddMinutes(-5),
            ExchangeId.Parse("Binance"),
            Symbol.Create("BTC-USDT"),
            Now.AddHours(4));
        return new OutboxMessage
        {
            Id = integrationEvent.EventId,
            Payload = OutboxEventSerializer.Serialize(integrationEvent),
            OccurredOnUtc = integrationEvent.OccurredOnUtc
        };
    }

    private async Task<OutboxMessage> ReadSingleAsync()
    {
        await using var context = await fixture.CreateDbContextAsync();
        return Assert.Single(await context.OutboxMessages.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
    }

    private sealed class StubPublisher(
        Func<FundingRateChangedIntegrationEvent, MessagePublishResult> publish) : IMessagePublisher
    {
        public int Calls { get; private set; }

        public Task<MessagePublishResult> PublishAsync<T>(
            string exchange,
            string routingKey,
            MessageEnvelope<T> message,
            IReadOnlyDictionary<string, object?>? headers,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(publish((FundingRateChangedIntegrationEvent)(object)message.Payload!));
        }
    }
}
