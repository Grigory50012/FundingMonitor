namespace FundingMonitor.Infrastructure.HistoricalRates.Messaging;

public interface IHistoricalRetryDelay
{
    Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken);
}
