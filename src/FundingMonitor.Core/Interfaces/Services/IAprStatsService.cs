using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Services;

/// <summary>
///     Сервис для расчёта APR статистики по периодам
/// </summary>
public interface IAprStatsService
{
    /// <summary>
    ///     Получить APR статистику по периодам для указанного символа
    /// </summary>
    /// <param name="symbol">Символ (например, BTC-USDT)</param>
    /// <param name="exchanges">Список бирж (опционально)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список APR статистики по периодам для каждой биржи</returns>
    Task<List<AprPeriodStats>> GetAprStatsAsync(
        string symbol,
        List<string>? exchanges,
        CancellationToken cancellationToken);
}