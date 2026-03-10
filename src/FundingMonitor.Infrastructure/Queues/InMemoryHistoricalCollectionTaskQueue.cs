using System.Collections.Concurrent;
using FundingMonitor.Core.Interfaces.Queues;
using FundingMonitor.Core.Queues;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Infrastructure.Queues;

public class InMemoryHistoricalCollectionTaskQueue : IHistoricalCollectionTaskQueue
{
    private readonly ConcurrentQueue<HistoricalCollectionTask> _historyTaskQueue = new();
    private readonly ILogger<InMemoryHistoricalCollectionTaskQueue> _logger;

    public InMemoryHistoricalCollectionTaskQueue(ILogger<InMemoryHistoricalCollectionTaskQueue> logger)
    {
        _logger = logger;
    }

    public int Count => _historyTaskQueue.Count;

    public void Enqueue(HistoricalCollectionTask task)
    {
        _historyTaskQueue.Enqueue(task);
        _logger.LogDebug("Добавлена задача в очередь: {Exchange}:{Symbol}",
            task.Exchange, task.NormalizedSymbol);
    }

    public bool TryDequeue(out HistoricalCollectionTask? task)
    {
        return _historyTaskQueue.TryDequeue(out task);
    }
}