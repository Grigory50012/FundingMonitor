
namespace FundingMonitor.Core.HistoricalRates.Interfaces;

/// <summary>
///     Сервис для сбора исторических данных о ставках финансирования
/// </summary>
public interface IHistoricalFundingRateCollector
{
    /// <summary>
    ///     Собрать исторические данные, сохранить в БД и обработать ошибки
    /// </summary>
    /// <param name="task">Задача на сбор данных</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task CollectAndSaveAsync(
        HistoricalCollectionTask task,
        CancellationToken cancellationToken);
}