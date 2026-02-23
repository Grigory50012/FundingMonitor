namespace FundingMonitor.Core.Entities;

public class HistoricalFundingRate
{
    public ExchangeType Exchange { get; set; }
    public string NormalizedSymbol { get; set; } = string.Empty;
    public decimal FundingRate { get; set; }
    public DateTime FundingTime { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}