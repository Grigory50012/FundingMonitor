namespace FundingMonitor.Data.Entities;

public class FundingRate
{
    public int Id { get; set; }
    public decimal Rate { get; set; }  // 0.00010000 = 0.01%
    public DateTime FundingTime { get; set; }
    public decimal? PredictedRate { get; set; }
    public decimal? MarkPrice { get; set; }
    public decimal? IndexPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int PairId { get; set; }
    public int ExchangeId { get; set; }
    
    public virtual TradingPair Pair { get; set; } = null!;
    public virtual Exchange Exchange { get; set; } = null!;
}