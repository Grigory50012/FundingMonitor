namespace FundingMonitor.Core.Events;

public class SymbolRemovedEvent : FundingEvent
{
    public DateTime RemovedAt { get; set; }
}