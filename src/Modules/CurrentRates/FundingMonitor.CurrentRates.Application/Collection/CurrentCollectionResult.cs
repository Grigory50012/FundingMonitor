namespace FundingMonitor.CurrentRates.Application.Collection;

public sealed record CurrentCollectionResult(
    int RatesCount,
    int EventsCount,
    int SuccessfulExchanges,
    int FailedExchanges);
