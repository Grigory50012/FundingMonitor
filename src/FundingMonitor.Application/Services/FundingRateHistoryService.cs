using FundingMonitor.Core.Events;
using FundingMonitor.Core.Interfaces.Queues;
using FundingMonitor.Core.Interfaces.Services;
using FundingMonitor.Core.Queues;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Application.Services;

public class FundingRateHistoryService : IFundingRateHistoryService
{
    private readonly ILogger<FundingRateHistoryService> _logger;
    private readonly IHistoricalCollectionTaskQueue _taskQueue;

    public FundingRateHistoryService(
        IHistoricalCollectionTaskQueue taskQueue,
        ILogger<FundingRateHistoryService> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
    }

    public Task EnqueueHistoricalCollectionTasksAsync(List<FundingRateEvent> events,
        CancellationToken cancellationToken)
    {
        foreach (var @event in events)
        {
            var task = @event switch
            {
                NewSymbolFundingEvent e => new HistoricalCollectionTask
                {
                    Exchange = e.Exchange,
                    NormalizedSymbol = e.NormalizedSymbol,
                    Type = HistoricalCollectionTaskType.CollectHistoryForNewSymbol
                },
                FundingTimeChangeEvent e => new HistoricalCollectionTask
                {
                    Exchange = e.Exchange,
                    NormalizedSymbol = e.NormalizedSymbol,
                    Type = HistoricalCollectionTaskType.RefreshHistoryAfterTimeChange
                },
                _ => null
            };

            if (task != null) _taskQueue.Enqueue(task);
        }

        _logger.LogInformation("Added {Count} tasks to history taskQueue (total in taskQueue: {QueueCount})",
            events.Count, _taskQueue.Count);

        return Task.CompletedTask;
    }
}