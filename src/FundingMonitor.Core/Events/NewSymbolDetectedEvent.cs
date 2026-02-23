namespace FundingMonitor.Core.Events;

public class NewSymbolDetectedEvent : FundingEvent
{
    public DateTime DetectedAt { get; set; }
    public int? FundingIntervalHours { get; set; }
    public DateTime? NextFundingTime { get; set; }
}