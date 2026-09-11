namespace FundingMonitor.HistoricalRates.Application;

public interface IHistoricalEventHandler
{
    Task HandleAsync(
        HistoricalCollectionRequest request,
        CancellationToken cancellationToken);
}
