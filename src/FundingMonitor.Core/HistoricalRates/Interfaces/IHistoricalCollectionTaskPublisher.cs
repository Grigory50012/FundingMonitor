
namespace FundingMonitor.Core.HistoricalRates.Interfaces;

public interface IHistoricalCollectionTaskPublisher
{
    Task PublishAsync(
        IReadOnlyCollection<FundingRateChangedEvent> events,
        CancellationToken cancellationToken);
}
