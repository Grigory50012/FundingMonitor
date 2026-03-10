using System.Collections.Concurrent;
using FundingMonitor.Core.Interfaces.Queues;
using FundingMonitor.Core.Queues;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Infrastructure.Queues;

public class InMemoryHistoricalCollectionQueue : IHistoricalCollectionQueue
{
    private readonly ILogger<InMemoryHistoricalCollectionQueue> _logger;
    private readonly ConcurrentQueue<HistoricalCollectionTask> _queue = new();

    public InMemoryHistoricalCollectionQueue(ILogger<InMemoryHistoricalCollectionQueue> logger)
    {
        _logger = logger;
    }

    public int Count => _queue.Count;

    public void Enqueue(HistoricalCollectionTask task)
    {
        _queue.Enqueue(task);
        _logger.LogDebug("Добавлена задача в очередь: {Exchange}:{Symbol}",
            task.Exchange, task.NormalizedSymbol);
    }

    public bool TryDequeue(out HistoricalCollectionTask? task)
    {
        return _queue.TryDequeue(out task);
    }
}