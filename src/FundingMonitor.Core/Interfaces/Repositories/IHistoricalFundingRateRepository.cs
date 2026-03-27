using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Repositories;

/// <summary>
///     Репозиторий для работы с историческими ставками финансирования
/// </summary>
public interface IHistoricalFundingRateRepository
{
    /// <summary>
    ///     Добавить список исторических ставок в базу данных
    /// </summary>
    /// <param name="rates">Список исторических ставок</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task AddRangeAsync(IEnumerable<HistoricalFundingRate> rates, CancellationToken cancellationToken);

    /// <summary>
    ///     Получить исторические ставки
    /// </summary>
    /// <param name="symbol">Символ (например, BTC-USDT)</param>
    /// <param name="exchanges">Список бирж (опционально)</param>
    /// <param name="from">Начальная дата (опционально)</param>
    /// <param name="to">Конечная дата (опционально)</param>
    /// <param name="limit">Максимальное количество записей</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список исторических ставок</returns>
    Task<List<HistoricalFundingRate>> GetHistoryAsync(
        string symbol,
        List<ExchangeType>? exchanges,
        DateTime? from,
        DateTime? to,
        int? limit,
        CancellationToken cancellationToken);
}