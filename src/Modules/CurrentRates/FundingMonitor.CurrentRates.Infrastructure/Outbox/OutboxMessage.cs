namespace FundingMonitor.CurrentRates.Infrastructure.Outbox;

internal sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public required string Payload { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; }
    public DateTimeOffset? ProcessedOnUtc { get; set; }
    public int Attempts { get; set; }
    public string? Error { get; set; }
}
