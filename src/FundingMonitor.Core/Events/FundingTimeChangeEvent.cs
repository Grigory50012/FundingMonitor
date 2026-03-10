namespace FundingMonitor.Core.Events;

public class FundingTimeChangeEvent : FundingRateEvent
{
    public DateTime OldFundingTime { get; set; }
    public DateTime NewFundingTime { get; set; }
    public DateTime PreviousCheckTime { get; set; }
}