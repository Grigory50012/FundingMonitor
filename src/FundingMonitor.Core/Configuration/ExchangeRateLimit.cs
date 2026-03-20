namespace FundingMonitor.Core.Configuration;

public class ExchangeRateLimit
{
    /// <summary>Количество запросов в секунду при включенном rate limit</summary>
    public double RequestsPerSecond { get; set; } = 10.0;

    /// <summary>Порог количества задач для включения rate limit (если задач больше — включаем)</summary>
    public int Threshold { get; set; } = 100;

    /// <summary>Задержка между запросами (мс) = 1000 / RequestsPerSecond</summary>
    public int DelayMilliseconds => (int)(1000.0 / RequestsPerSecond);
}