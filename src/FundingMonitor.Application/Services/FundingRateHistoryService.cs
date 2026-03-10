using FundingMonitor.Core.Events;
using FundingMonitor.Core.Interfaces.Queues;
using FundingMonitor.Core.Interfaces.Services;
using FundingMonitor.Core.Queues;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Application.Services;

public class FundingRateHistoryService : IFundingRateHistoryService
{
    private readonly ILogger<FundingRateHistoryService> _logger;
    private readonly IHistoricalCollectionQueue _queue;

    public FundingRateHistoryService(
        IHistoricalCollectionQueue queue,
        ILogger<FundingRateHistoryService> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    public Task ProcessDetectionEventsAsync(List<FundingEvent> events, CancellationToken cancellationToken)
    {
        if (events.Count == 0) return Task.CompletedTask;

        foreach (var @event in events)
        {
            var task = @event switch
            {
                NewSymbolDetectedEvent e => new HistoricalCollectionTask
                {
                    Exchange = e.Exchange,
                    NormalizedSymbol = e.NormalizedSymbol,
                    Type = HistoricalCollectionTaskType.NewSymbol
                },
                FundingTimeChangedEvent e => new HistoricalCollectionTask
                {
                    Exchange = e.Exchange,
                    NormalizedSymbol = e.NormalizedSymbol,
                    Type = HistoricalCollectionTaskType.FundingTimeChanged
                },
                _ => null
            };

            if (task != null) _queue.Enqueue(task);
        }

        _logger.LogInformation("Добавлено {Count} задач в очередь истории (всего в очереди: {QueueCount})",
            events.Count, _queue.Count);

        return Task.CompletedTask;
    }
}