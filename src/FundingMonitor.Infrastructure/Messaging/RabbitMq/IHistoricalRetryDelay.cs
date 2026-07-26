namespace FundingMonitor.Infrastructure.Messaging.RabbitMq;

public interface IHistoricalRetryDelay
{
    Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken);
}
