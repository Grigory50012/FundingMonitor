namespace FundingMonitor.HistoricalRates.Infrastructure.Messaging;

internal sealed class HistoricalRetryDelay : IHistoricalRetryDelay
{
    public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, cancellationToken);
}
