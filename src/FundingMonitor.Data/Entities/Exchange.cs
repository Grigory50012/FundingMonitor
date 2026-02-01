namespace FundingMonitor.Data.Entities;

public class Exchange
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual ICollection<TradingPair> TradingPairs { get; set; } = new List<TradingPair>();
    public virtual ICollection<FundingRate> FundingRates { get; set; } = new List<FundingRate>();
}