namespace FundingMonitor.Infrastructure.HistoricalRates.Messaging;

public sealed class HistoricalRetryDelay : IHistoricalRetryDelay
{
    public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        return Task.Delay(delay, cancellationToken);
    }
}
