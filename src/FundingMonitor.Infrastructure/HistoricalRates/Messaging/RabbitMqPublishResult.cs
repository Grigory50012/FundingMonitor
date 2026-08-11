namespace FundingMonitor.Infrastructure.HistoricalRates.Messaging;

public sealed record RabbitMqPublishResult(
    bool Confirmed,
    bool Returned,
    string? Error)
{
    public static RabbitMqPublishResult Success { get; } = new(true, false, null);

    public static RabbitMqPublishResult NotConfirmed(string error) =>
        new(false, false, error);

    public static RabbitMqPublishResult Unroutable(string error) =>
        new(true, true, error);
}
