using FundingMonitor.Core.Queues;

namespace FundingMonitor.Core.Interfaces.Queues;

/// <summary>
///     Интерфейс персистентной очереди задач на сбор истории
/// </summary>
public interface IHistoryTaskQueue
{
    /// <summary>
    ///     Количество задач в очереди
    /// </summary>
    int Count { get; }

    /// <summary>
    ///     Добавить задачу в очередь
    /// </summary>
    Task EnqueueAsync(HistoricalCollectionTask task, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Извлечь задачу из очереди (блокирующая операция)
    /// </summary>
    Task<HistoricalCollectionTask?> DequeueAsync(CancellationToken cancellationToken = default);
}