
namespace FundingMonitor.Core.HistoricalRates.Interfaces;

public interface IHistoricalCollectionFailureClassifier
{
    HistoricalCollectionFailureDecision Classify(
        Exception exception,
        CancellationToken applicationStoppingToken);
}
