namespace FundingMonitor.HistoricalRates.Infrastructure.Inbox;

internal sealed class InboxMessage
{
    public Guid EventId { get; init; }
    public required string Consumer { get; init; }
    public DateTimeOffset ProcessedOnUtc { get; init; }
}
