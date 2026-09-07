namespace FundingMonitor.CurrentRates.Application.Collection;

public sealed record CurrentCollectionResult(
    bool Success,
    int RatesCount,
    int EventsCount,
    int SuccessfulExchanges,
    int FailedExchanges);
