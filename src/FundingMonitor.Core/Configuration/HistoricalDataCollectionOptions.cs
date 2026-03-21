namespace FundingMonitor.Core.Configuration;

public class HistoricalDataCollectionOptions
{
    public const string SectionName = "HistoricalDataCollectionOptions";

    /// <summary>Максимум параллельных задач сбора истории</summary>
    public int MaxConcurrentTasks { get; set; } = 10;

    /// <summary>Размер страницы API</summary>
    public int ApiPageSize { get; set; } = 1000;

    /// <summary>Сколько месяцев истории собирать для новых символов</summary>
    public int MaxHistoryMonths { get; set; } = 1;

    /// <summary>Максимум попыток при ошибке</summary>
    public int MaxRetries { get; set; } = 3;
}