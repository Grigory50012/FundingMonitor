namespace FundingMonitor.Core.Events;

public class NewSymbolFundingEvent : FundingRateEvent
{
    public DateTime DetectedAt { get; set; }
    public int? FundingIntervalHours { get; set; }
    public DateTime? NextFundingTime { get; set; }
}