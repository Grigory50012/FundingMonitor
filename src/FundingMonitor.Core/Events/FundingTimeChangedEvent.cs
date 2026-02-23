namespace FundingMonitor.Core.Events;

public class FundingTimeChangedEvent : FundingEvent
{
    public DateTime OldFundingTime { get; set; }
    public DateTime NewFundingTime { get; set; }
    public DateTime PreviousCheckTime { get; set; }
}