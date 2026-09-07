using System.Diagnostics.Metrics;
using FundingMonitor.CurrentRates.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FundingMonitor.CurrentRates.Infrastructure.Outbox;

internal sealed class OutboxMetrics : IDisposable
{
    private readonly IDbContextFactory<CurrentRatesDbContext> _contextFactory;
    private readonly Meter _meter = new("FundingMonitor.CurrentRates");
    private readonly TimeProvider _timeProvider;
    private long _oldestSeconds;
    private long _pending;

    public OutboxMetrics(
        IDbContextFactory<CurrentRatesDbContext> contextFactory,
        TimeProvider timeProvider)
    {
        _contextFactory = contextFactory;
        _timeProvider = timeProvider;
        _meter.CreateObservableGauge(
            "fundingmonitor.outbox.pending",
            () => Interlocked.Read(ref _pending));
        _meter.CreateObservableGauge(
            "fundingmonitor.outbox.oldest_seconds",
            () => Interlocked.Read(ref _oldestSeconds));
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var pendingQuery = dbContext.OutboxMessages
            .AsNoTracking()
            .Where(item => item.ProcessedOnUtc == null);
        var pending = await pendingQuery.LongCountAsync(cancellationToken);
        var oldest = await pendingQuery
            .Select(item => (DateTimeOffset?)item.OccurredOnUtc)
            .MinAsync(cancellationToken);
        var oldestSeconds = oldest is null
            ? 0
            : Math.Max(0, (long)(_timeProvider.GetUtcNow() - oldest.Value).TotalSeconds);

        Interlocked.Exchange(ref _pending, pending);
        Interlocked.Exchange(ref _oldestSeconds, oldestSeconds);
    }

    public void Dispose() => _meter.Dispose();
}
