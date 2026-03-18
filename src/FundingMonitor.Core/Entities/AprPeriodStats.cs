namespace FundingMonitor.Core.Entities;

/// <summary>
///     Статистика APR по определённому периоду для конкретной биржи
/// </summary>
public class AprPeriodStats
{
    /// <summary>
    ///     Название биржи
    /// </summary>
    public string Exchange { get; set; } = string.Empty;

    /// <summary>
    ///     Название периода (например, "1 день", "7 дней")
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    ///     Количество дней в периоде
    /// </summary>
    public int Days { get; set; }

    /// <summary>
    ///     Годовая процентная ставка (APR) в процентах
    /// </summary>
    public decimal Apr { get; set; }

    /// <summary>
    ///     Суммарная ставка финансирования за период в процентах
    /// </summary>
    public decimal TotalFundingRatePercent { get; set; }

    /// <summary>
    ///     Количество выплат за период
    /// </summary>
    public int PaymentsCount { get; set; }

    /// <summary>
    ///     Средняя ставка за выплату в процентах
    /// </summary>
    public decimal AvgFundingRatePercent { get; set; }
}