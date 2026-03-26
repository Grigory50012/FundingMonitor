namespace FundingMonitor.Core.Entities;

/// <summary>
///     Статистика APR по определённому периоду для конкретной биржи
/// </summary>
public record AprPeriodStats
{
    /// <summary>
    ///     Название биржи
    /// </summary>
    public string Exchange { get; init; } = string.Empty;

    /// <summary>
    ///     Название периода (например, "1 день", "7 дней")
    /// </summary>
    public string Period { get; init; } = string.Empty;

    /// <summary>
    ///     Количество дней в периоде
    /// </summary>
    public int Days { get; init; }

    /// <summary>
    ///     Годовая процентная ставка (APR) в процентах
    /// </summary>
    public decimal Apr { get; init; }

    /// <summary>
    ///     Суммарная ставка финансирования за период в процентах
    /// </summary>
    public decimal TotalFundingRatePercent { get; init; }

    /// <summary>
    ///     Количество выплат за период
    /// </summary>
    public int PaymentsCount { get; init; }

    /// <summary>
    ///     Средняя ставка за выплату в процентах
    /// </summary>
    public decimal AvgFundingRatePercent { get; init; }
}