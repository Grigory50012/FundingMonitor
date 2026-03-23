using FundingMonitor.Core.Events;
using FundingMonitor.Core.Interfaces.Queues;
using FundingMonitor.Core.Interfaces.Services;
using FundingMonitor.Core.Queues;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Application.Services;

/// <summary>
///     Продюсер задач для сбора истории
/// </summary>
public class HistoricalCollectionProducer : IHistoricalCollectionProducer
{
    private readonly ILogger<HistoricalCollectionProducer> _logger;
    private readonly IHistoryTaskQueue _taskQueue;

    public HistoricalCollectionProducer(
        IHistoryTaskQueue taskQueue,
        ILogger<HistoricalCollectionProducer> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
    }

    public async Task EnqueueHistoricalCollectionTasksAsync(List<FundingRateChangedEvent> events,
        CancellationToken cancellationToken)
    {
        foreach (var @event in events)
        {
            var task = new HistoricalCollectionTask
            {
                Exchange = @event.Exchange,
                NormalizedSymbol = @event.NormalizedSymbol
            };

            await _taskQueue.EnqueueAsync(task, cancellationToken);
        }

        _logger.LogInformation("Added {Count} tasks to history queue (total: {QueueCount})",
            events.Count, _taskQueue.Count);
    }
}