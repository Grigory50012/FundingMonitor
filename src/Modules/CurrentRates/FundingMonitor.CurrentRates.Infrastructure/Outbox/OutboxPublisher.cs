using System.Diagnostics;
using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates.Infrastructure.Persistence;
using FundingMonitor.Messaging;
using FundingMonitor.Messaging.Abstractions;
using FundingMonitor.SharedKernel;
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
    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var pending = await ReadPendingAsync(cancellationToken);
        var published = 0;
        foreach (var message in pending)
            if (await PublishAsync(message, cancellationToken)) published++;

        await metrics.RefreshAsync(cancellationToken);
        if (pending.Count > 0)
            logger.LogInformation("Outbox batch completed: {Attempted} attempted, {Published} published, {Failed} retained in {DurationMs} ms",
                pending.Count, published, pending.Count - published, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        return pending.Count;
    }

    // One backend instance runs one sequential publisher loop; no distributed claiming is needed.
    private async Task<IReadOnlyList<OutboxMessage>> ReadPendingAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(item => item.ProcessedOnUtc == null)
            .OrderBy(item => item.OccurredOnUtc)
            .ThenBy(item => item.Id)
            .Take(Math.Clamp(options.Value.BatchSize, 1, 1000))
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        using var logScope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["EventId"] = message.Id, ["Exchange"] = message.Exchange,
            ["Symbol"] = message.Symbol, ["Attempt"] = message.Attempts + 1
        });
        string? failure = null;
        Exception? failureException = null;
        try
        {
            var integrationEvent = new FundingRateChangedIntegrationEvent(
                message.Id,
                message.OccurredOnUtc,
                ExchangeId.Parse(message.Exchange),
                Symbol.Create(message.Symbol),
                message.NextFundingTime);
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
                logger.LogDebug("Outbox event published");
                return true;
            }

            failure = result.Error ?? (result.Returned
                ? "Published message was returned."
                : "Publisher confirm was not received.");
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            failure = exception.Message;
            failureException = exception;
        }

        await MarkFailedAsync(message.Id, failure, cancellationToken);
        logger.LogWarning(failureException, "Outbox publication failed; event retained for retry: {Reason}", failure);
        return false;
    }

    private async Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.OutboxMessages
            .Where(item => item.Id == id)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.ProcessedOnUtc, timeProvider.GetUtcNow())
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
            .Where(item => item.Id == id)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.Attempts, item => item.Attempts + 1)
                    .SetProperty(item => item.Error, boundedError),
                cancellationToken);
    }
}
