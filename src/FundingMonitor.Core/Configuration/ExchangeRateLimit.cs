namespace FundingMonitor.Core.Configuration;

public class ExchangeRateLimit
{
    /// <summary>Максимальное количество запросов в окне</summary>
    public int PermitLimit { get; set; } = 1200;

    /// <summary>Размер окна в секундах</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>Количество сегментов в окне (для сглаживания)</summary>
    public int SegmentsPerWindow { get; set; } = 12;

    /// <summary>Размер очереди ожидания</summary>
    public int QueueLimit { get; set; } = 100;
}