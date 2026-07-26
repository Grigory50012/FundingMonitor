namespace FundingMonitor.Infrastructure.Messaging.RabbitMq;

public sealed class HistoricalRetryDelay : IHistoricalRetryDelay
{
    public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        return Task.Delay(delay, cancellationToken);
    }
}
