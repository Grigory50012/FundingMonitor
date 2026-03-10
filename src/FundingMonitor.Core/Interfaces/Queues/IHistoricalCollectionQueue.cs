using FundingMonitor.Core.Queues;

namespace FundingMonitor.Core.Interfaces.Queues;

public interface IHistoricalCollectionQueue
{
    int Count { get; }
    void Enqueue(HistoricalCollectionTask task);
    bool TryDequeue(out HistoricalCollectionTask? task);
}