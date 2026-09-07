namespace FundingMonitor.CurrentRates.Infrastructure.Outbox;

internal sealed class OutboxOptions
{
    public const string SectionName = "CurrentRatesOutbox";

    public int PollIntervalSeconds { get; set; } = 2;
    public int BatchSize { get; set; } = 100;
    public int LeaseSeconds { get; set; } = 30;
}
