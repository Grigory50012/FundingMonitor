namespace FundingMonitor.Core.Models;

// Модель для ставки финансирования
public class FundingRateInfo
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Rate { get; set; }                          // 0.0001 = 0.01%
    public decimal? PredictedRate { get; set; }
    public DateTime NextFundingTime { get; set; }
    public decimal MarkPrice { get; set; }
    public decimal IndexPrice { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}