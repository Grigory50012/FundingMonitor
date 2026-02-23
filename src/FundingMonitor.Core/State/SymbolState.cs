namespace FundingMonitor.Core.State;

public class SymbolState
{
    public string NormalizedSymbol { get; set; } = string.Empty;
    public DateTime? NextFundingTime { get; set; }
    public decimal FundingRate { get; set; }
    public int? FundingIntervalHours { get; set; }
    public DateTime LastCheck { get; set; }
    public bool IsActive { get; set; } = true;
}