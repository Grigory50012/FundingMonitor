namespace FundingMonitor.Core.Configuration;

public class CurrentDataCollectionOptions
{
    public const string SectionName = "CurrentDataCollectionOptions";

    /// <summary>
    ///     Интервал запуска фонового сбора текущих funding rates.
    /// </summary>
    public int UpdateIntervalSeconds { get; set; } = 10;

    /// <summary>
    ///     Через сколько минут без успешного наблюдения пара переводится в IsActive=false.
    /// </summary>
    public int DeactivateMissingAfterMinutes { get; set; } = 5;
}
