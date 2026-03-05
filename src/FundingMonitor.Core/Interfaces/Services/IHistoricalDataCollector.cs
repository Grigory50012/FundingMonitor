using FundingMonitor.Core.Events;

namespace FundingMonitor.Core.Interfaces.Services;

/// <summary>
///     Отвечает за сбор исторических данных при обнаружении изменений
/// </summary>
public interface IHistoricalDataCollector
{
    /// <summary>
    ///     Обрабатывает события и инициирует сбор исторических данных
    /// </summary>
    /// <param name="events">Список событий для обработки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task ProcessEventsAsync(List<FundingEvent> events, CancellationToken cancellationToken);
}