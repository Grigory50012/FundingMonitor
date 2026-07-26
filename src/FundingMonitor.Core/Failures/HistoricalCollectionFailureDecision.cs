namespace FundingMonitor.Core.Failures;

public enum HistoricalCollectionFailureDecision
{
    Retry,
    DeadLetter,
    AbandonForShutdown
}
