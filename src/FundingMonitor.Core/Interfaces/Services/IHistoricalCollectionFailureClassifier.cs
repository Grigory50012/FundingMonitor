using FundingMonitor.Core.Failures;

namespace FundingMonitor.Core.Interfaces.Services;

public interface IHistoricalCollectionFailureClassifier
{
    HistoricalCollectionFailureDecision Classify(
        Exception exception,
        CancellationToken applicationStoppingToken);
}
