using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Repositories;

/// <summary>
///     Репозиторий для работы с текущими ставками финансирования
/// </summary>
public interface ICurrentFundingRateRepository
{
    /// <summary>
    ///     Обновить текущие ставки в базе данных
    /// </summary>
    /// <param name="rates">Список ставок для обновления</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task UpdateAsync(IEnumerable<CurrentFundingRate> rates, CancellationToken cancellationToken);

    /// <summary>
    ///     Получить текущие ставки
    /// </summary>
    /// <param name="symbol">Символ (опционально, например: BTC)</param>
    /// <param name="exchanges">Список бирж (опционально)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список текущих ставок</returns>
    Task<IEnumerable<CurrentFundingRate>> GetRatesAsync(
        string? symbol,
        List<ExchangeType>? exchanges,
        CancellationToken cancellationToken);
}