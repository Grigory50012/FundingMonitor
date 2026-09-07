using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates.Infrastructure.Outbox;
using FundingMonitor.CurrentRates.IntegrationTests.Infrastructure;
using FundingMonitor.Messaging;
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

    [Fact]
    public async Task DatabaseCommitAndUnconfirmedPublish_LeavesPending()
    {
        await fixture.ResetAsync();
        await fixture.SeedOutboxAsync(CreateMessage());
        var publisher = new StubPublisher(_ => new MessagePublishResult(false, false, "not confirmed"));
        await using var scope = await fixture.CreatePublisherScopeAsync(publisher, Now);

        await scope.Publisher.ProcessPendingAsync(TestContext.Current.CancellationToken);

        var message = await ReadSingleAsync();
        Assert.Null(message.ProcessedOnUtc);
        Assert.Equal(1, message.Attempts);
        Assert.Null(message.LockOwner);
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
    public async Task ExpiredLease_AnotherPublisherCanClaimTheRow()
    {
        await fixture.ResetAsync();
        await fixture.SeedOutboxAsync(CreateMessage(
            lockedUntilUtc: Now.AddMinutes(-1),
            lockOwner: "dead-instance"));
        var publisher = new StubPublisher(_ => MessagePublishResult.Success);
        await using var scope = await fixture.CreatePublisherScopeAsync(publisher, Now);

        await scope.Publisher.ProcessPendingAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, publisher.Calls);
        Assert.NotNull((await ReadSingleAsync()).ProcessedOnUtc);
    }

    private static readonly DateTimeOffset Now =
        new(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

    private static OutboxMessage CreateMessage(
        DateTimeOffset? lockedUntilUtc = null,
        string? lockOwner = null)
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
            Type = typeof(FundingRateChangedIntegrationEvent).FullName!,
            Payload = OutboxEventSerializer.Serialize(integrationEvent),
            OccurredOnUtc = integrationEvent.OccurredOnUtc,
            LockedUntilUtc = lockedUntilUtc,
            LockOwner = lockOwner
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
