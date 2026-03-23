using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Events;

/// <summary>
///     Событие изменения данных о ставке финансирования
///     (новый символ или изменение времени выплаты)
/// </summary>
public class FundingRateChangedEvent
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
    ///     Время обнаружения события
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    ///     Интервал выплат в часах
    /// </summary>
    public int? FundingIntervalHours { get; set; }

    /// <summary>
    ///     Время следующей выплаты
    /// </summary>
    public DateTime? NextFundingTime { get; set; }
}