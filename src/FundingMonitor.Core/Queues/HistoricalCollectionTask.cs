using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Queues;

public class HistoricalCollectionTask
{
    public ExchangeType Exchange { get; init; }
    public string NormalizedSymbol { get; init; } = string.Empty;
    public HistoricalCollectionTaskType Type { get; init; }
    public int RetryCount { get; set; }
}