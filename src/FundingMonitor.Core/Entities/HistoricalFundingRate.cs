namespace FundingMonitor.Core.Entities;

/// <summary>
///     Исторические данные начисления ставки финансирования
/// </summary>
public record HistoricalFundingRate
{
    /// <summary>
    ///     Название биржи
    /// </summary>
    public required ExchangeType Exchange { get; init; }

    /// <summary>
    ///     Символ, например "BTC-USDT"
    /// </summary>
    public required string NormalizedSymbol { get; init; } = string.Empty;

    /// <summary>
    ///     Ставка финансирования
    /// </summary>
    public required decimal FundingRate { get; init; }

    /// <summary>
    ///     Время выплаты
    /// </summary>
    public required DateTime FundingTime { get; init; }

    /// <summary>
    ///     Время сбора данных
    /// </summary>
    public required DateTime CollectedAt { get; init; } = DateTime.UtcNow;
}