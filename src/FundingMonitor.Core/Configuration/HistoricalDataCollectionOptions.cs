namespace FundingMonitor.Core.Configuration;

public class HistoricalDataCollectionOptions
{
    public const string SectionName = "HistoricalDataCollection";

    /// <summary>
    ///     Размер страницы при запросе к API
    /// </summary>
    public int ApiPageSize { get; set; } = 200;

    /// <summary>
    ///     Количество параллельных задач
    /// </summary>
    public int BatchSize { get; set; } = 5;

    /// <summary>
    ///     Сколько месяцев истории загружать для новых символов
    /// </summary>
    public int MaxHistoryMonths { get; set; } = 1;
}