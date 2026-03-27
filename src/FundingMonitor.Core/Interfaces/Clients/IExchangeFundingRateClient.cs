using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Clients;

/// <summary>
///     Клиент для получения данных от биржи
/// </summary>
public interface IExchangeFundingRateClient
{
    /// <summary>
    ///     Тип биржи
    /// </summary>
    ExchangeType ExchangeType { get; }

    /// <summary>
    ///     Получить текущие ставки финансирования
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список текущих ставок</returns>
    Task<List<CurrentFundingRate>> GetCurrentFundingRatesAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Получить исторические ставки финансирования
    /// </summary>
    /// <param name="symbol">Символ (например, BTC-USDT)</param>
    /// <param name="fromTime">Начальная дата</param>
    /// <param name="toTime">Конечная дата</param>
    /// <param name="limit">Максимальное количество записей</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список исторических ставок</returns>
    Task<List<HistoricalFundingRate>> GetHistoricalFundingRatesAsync(
        string symbol,
        DateTime fromTime,
        DateTime toTime,
        int limit,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Проверить доступность биржи
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>True, если биржа доступна</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
}