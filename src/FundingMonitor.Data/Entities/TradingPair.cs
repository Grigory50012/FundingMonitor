namespace FundingMonitor.Data.Entities;

public class TradingPair
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;  // BTCUSDT
    public string BaseAsset { get; set; } = string.Empty;  // BTC
    public string QuoteAsset { get; set; } = string.Empty; // USDT
    public bool IsActive { get; set; } = true;
    public DateTime? ListedAt { get; set; }
    public DateTime? LastSeen { get; set; }
    
    public int ExchangeId { get; set; }
    
    public virtual Exchange Exchange { get; set; } = null!;
    public virtual ICollection<FundingRate> FundingRates { get; set; } = new List<FundingRate>();
}