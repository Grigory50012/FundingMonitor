using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Queues;

/// <summary>
///     Задача на сбор исторических данных
/// </summary>
public record HistoricalCollectionTask
{
    /// <summary>
    ///     Биржа
    /// </summary>
    public ExchangeType Exchange { get; init; }

    /// <summary>
    ///     Нормализованный символ (например, BTC-USDT)
    /// </summary>
    public string NormalizedSymbol { get; init; } = string.Empty;

    /// <summary>
    ///     Количество попыток
    /// </summary>
    public int RetryCount { get; set; }
}