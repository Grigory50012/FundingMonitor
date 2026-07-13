using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Repositories;

/// <summary>
///     Репозиторий для работы с текущими ставками финансирования
/// </summary>
public interface ICurrentFundingRateRepository
{
    /// <summary>
    ///     Обновить текущие ставки одной биржи в базе данных
    /// </summary>
    /// <param name="exchange">Биржа, для которой получен snapshot</param>
    /// <param name="rates">Список ставок для обновления</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task UpdateExchangeAsync(
        ExchangeType exchange,
        IEnumerable<CurrentFundingRate> rates,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Отключить устаревшие ставки биржи без обновления свежести
    /// </summary>
    /// <param name="exchange">Биржа, сбор которой не подтвердил свежие данные</param>
    /// <param name="deactivateMissingAfter">Интервал, после которого пары становятся неактивными</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task DeactivateStaleAsync(
        ExchangeType exchange,
        TimeSpan deactivateMissingAfter,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Получить текущие ставки.
    /// </summary>
    /// <param name="symbol">Символ (опционально, например: BTC)</param>
    /// <param name="exchanges">Список бирж (опционально)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список активных текущих ставок</returns>
    Task<IEnumerable<CurrentFundingRate>> GetRatesAsync(
        string? symbol,
        List<ExchangeType>? exchanges,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Получить все текущие ставки, включая неактивные. Используется сборщиком для сравнения снимков.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Полное сохранённое состояние текущих ставок</returns>
    Task<IEnumerable<CurrentFundingRate>> GetAllRatesAsync(CancellationToken cancellationToken);
}
