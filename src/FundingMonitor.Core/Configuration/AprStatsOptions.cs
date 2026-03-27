namespace FundingMonitor.Core.Configuration;

/// <summary>
///     Настройки для сервиса APR статистики
/// </summary>
public class AprStatsOptions
{
    public const string SectionName = "AprStats";

    /// <summary>
    ///     Время жизни кэша в минутах
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 5;

    /// <summary>
    ///     Максимальная глубина истории (дни)
    /// </summary>
    public int MaxHistoryDays { get; set; } = 30;

    /// <summary>
    ///     Периоды для расчёта (дни)
    /// </summary>
    public List<int> Periods { get; set; } = [1, 2, 3, 7, 14, 21, 30];
}