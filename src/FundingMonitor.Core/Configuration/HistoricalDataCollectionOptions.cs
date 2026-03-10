namespace FundingMonitor.Core.Configuration;

public class HistoricalDataCollectionOptions
{
    public const string SectionName = "HistoricalDataCollectionOptions";

    /// <summary>Максимум параллельных задач сбора истории</summary>
    public int MaxConcurrentTasks { get; set; } = 10;

    /// <summary>Размер батча для обработки из очереди</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>Размер страницы API</summary>
    public int ApiPageSize { get; set; } = 200;

    /// <summary>Таймаут запроса к API (секунды)</summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>Сколько месяцев истории собирать для новых символов</summary>
    public int MaxHistoryMonths { get; set; } = 1;

    /// <summary>Максимум попыток при ошибке</summary>
    public int MaxRetries { get; set; } = 3;
}