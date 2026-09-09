using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates.Infrastructure.Persistence;
using FundingMonitor.Messaging;
using FundingMonitor.Messaging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.CurrentRates.Infrastructure.Outbox;

internal sealed class OutboxPublisher(
    IDbContextFactory<CurrentRatesDbContext> contextFactory,
    IMessagePublisher publisher,
    TimeProvider timeProvider,
    IOptions<OutboxOptions> options,
    OutboxMetrics metrics,
    ILogger<OutboxPublisher> logger)
{
    private readonly string _owner = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var claimed = await ClaimAsync(cancellationToken);
        foreach (var message in claimed)
            await PublishAsync(message, cancellationToken);

        await metrics.RefreshAsync(cancellationToken);
        return claimed.Count;
    }

    private async Task<IReadOnlyList<OutboxMessage>> ClaimAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var batchSize = Math.Clamp(options.Value.BatchSize, 1, 1000);
        var messages = await dbContext.OutboxMessages
            .FromSqlInterpolated($$"""
                SELECT *
                FROM current_rates."OutboxMessages"
                WHERE "ProcessedOnUtc" IS NULL
                  AND ("LockedUntilUtc" IS NULL OR "LockedUntilUtc" < {{now}})
                ORDER BY "OccurredOnUtc"
                LIMIT {{batchSize}}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        var lockedUntil = now.AddSeconds(Math.Max(1, options.Value.LeaseSeconds));
        foreach (var message in messages)
        {
            message.LockOwner = _owner;
            message.LockedUntilUtc = lockedUntil;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return messages;
    }

    private async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        string? failure = null;
        try
        {
            var integrationEvent = OutboxEventSerializer.Deserialize(message.Payload);
            var envelope = new MessageEnvelope<FundingRateChangedIntegrationEvent>(
                message.Id,
                message.Id,
                null,
                message.OccurredOnUtc,
                "funding-rate.changed",
                1,
                integrationEvent);
            var result = await publisher.PublishAsync(
                "current-rates.events",
                $"funding-rate.changed.{integrationEvent.Exchange.Value.ToLowerInvariant()}",
                envelope,
                null,
                cancellationToken);

            if (result.Confirmed && !result.Returned)
            {
                await MarkProcessedAsync(message.Id, cancellationToken);
                return;
            }

            failure = result.Error ?? (result.Returned
                ? "Published message was returned."
                : "Publisher confirm was not received.");
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            failure = exception.Message;
            logger.LogError(exception, "Outbox publication failed for {MessageId}", message.Id);
        }

        await MarkFailedAsync(message.Id, failure, cancellationToken);
    }

    private async Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.OutboxMessages
            .Where(item => item.Id == id && item.LockOwner == _owner)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.ProcessedOnUtc, timeProvider.GetUtcNow())
                    .SetProperty(item => item.LockedUntilUtc, (DateTimeOffset?)null)
                    .SetProperty(item => item.LockOwner, (string?)null)
                    .SetProperty(item => item.Error, (string?)null),
                cancellationToken);
    }

    private async Task MarkFailedAsync(
        Guid id,
        string? error,
        CancellationToken cancellationToken)
    {
        var boundedError = string.IsNullOrWhiteSpace(error)
            ? "Unknown publication failure."
            : error[..Math.Min(error.Length, 1000)];
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.OutboxMessages
            .Where(item => item.Id == id && item.LockOwner == _owner)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.Attempts, item => item.Attempts + 1)
                    .SetProperty(item => item.Error, boundedError)
                    .SetProperty(item => item.LockedUntilUtc, (DateTimeOffset?)null)
                    .SetProperty(item => item.LockOwner, (string?)null),
                cancellationToken);
    }
}
