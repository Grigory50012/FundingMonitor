namespace FundingMonitor.CurrentRates.Application.Collection;

public interface ICurrentFundingRateCollector
{
    Task<CurrentCollectionResult> CollectAsync(CancellationToken cancellationToken);
}
