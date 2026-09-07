namespace FundingMonitor.Messaging;

public readonly record struct MessagePublishResult(
    bool Confirmed,
    bool Returned,
    string? Error)
{
    public static MessagePublishResult Success => new(true, false, null);
}