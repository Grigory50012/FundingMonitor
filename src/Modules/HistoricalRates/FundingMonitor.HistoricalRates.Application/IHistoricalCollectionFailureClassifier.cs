namespace FundingMonitor.HistoricalRates.Application;

public interface IHistoricalCollectionFailureClassifier
{
    HistoricalCollectionFailureDecision Classify(
        Exception exception,
        CancellationToken applicationStoppingToken);
}
