namespace FundingMonitor.Core.HistoricalRates.Failures;

public enum HistoricalCollectionFailureDecision
{
    Retry,
    DeadLetter,
    AbandonForShutdown
}
