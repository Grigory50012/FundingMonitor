using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Events;

public abstract class FundingRateEvent
{
    public ExchangeType Exchange { get; init; }
    public string NormalizedSymbol { get; init; } = string.Empty;
}