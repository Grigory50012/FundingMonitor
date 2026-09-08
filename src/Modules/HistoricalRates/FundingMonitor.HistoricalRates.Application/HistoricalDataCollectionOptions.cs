using FundingMonitor.SharedKernel;

namespace FundingMonitor.HistoricalRates.Application;

public sealed class HistoricalDataCollectionOptions
{
    public const string SectionName = "HistoricalDataCollectionOptions";

    public int DefaultConcurrencyPerExchange { get; init; } = 10;
    public Dictionary<string, int> ExchangeConcurrency { get; init; } = [];
    public int ApiPageSize { get; init; } = 1000;
    public int MaxHistoryMonths { get; init; } = 1;
    public int MaxRetries { get; init; } = 3;
    public int RetryDelaySeconds { get; init; } = 30;

    public int GetConcurrency(ExchangeId exchange) =>
        ExchangeConcurrency.TryGetValue(exchange.Value, out var value)
            ? value
            : DefaultConcurrencyPerExchange;
}
