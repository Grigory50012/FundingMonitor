namespace FundingMonitor.HistoricalRates.Infrastructure.Messaging;

internal interface IHistoricalRetryDelay
{
    Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken);
}
