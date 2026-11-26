namespace FundingMonitor.CurrentRates.PublishPendingEvents;

internal sealed class OutboxOptions
{
    public const string SectionName = "CurrentRatesOutbox";

    public int PollIntervalSeconds { get; set; } = 2;
    public int BatchSize { get; set; } = 100;
}
