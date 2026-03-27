using FundingMonitor.Core.Events;

namespace FundingMonitor.Core.Interfaces.Services;

/// <summary>
///     Сервис для создания задач на сбор исторических данных
/// </summary>
public interface IHistoricalCollectionProducer
{
    /// <summary>
    ///     Создать задачи на сбор исторических данных для событий
    /// </summary>
    /// <param name="events">Список событий изменения ставок</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task EnqueueHistoricalCollectionTasksAsync(
        List<FundingRateChangedEvent> events,
        CancellationToken cancellationToken);
}