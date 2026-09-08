namespace FundingMonitor.HistoricalRates.Application;

public enum HistoricalCollectionFailureDecision
{
    Retry,
    DeadLetter,
    AbandonForShutdown
}
