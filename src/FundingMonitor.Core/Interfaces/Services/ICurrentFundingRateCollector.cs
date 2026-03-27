using FundingMonitor.Core.Results;

namespace FundingMonitor.Core.Interfaces.Services;

/// <summary>
///     Сервис для сбора текущих ставок финансирования с бирж
/// </summary>
public interface ICurrentFundingRateCollector
{
    /// <summary>
    ///     Собрать текущие ставки, сохранить в БД и отправить события
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат сбора</returns>
    Task<CurrentCollectionResult> CollectFundingRatesAsync(CancellationToken cancellationToken);
}