using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Events;

public abstract class FundingEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public ExchangeType Exchange { get; init; }
    public string NormalizedSymbol { get; init; } = string.Empty;
}