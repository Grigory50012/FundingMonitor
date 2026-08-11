
namespace FundingMonitor.Core.HistoricalRates.Configuration;

public sealed class HistoricalDataCollectionOptions
{
    public const string SectionName = "HistoricalDataCollectionOptions";

    public int DefaultConcurrencyPerExchange { get; init; } = 10;
    public Dictionary<ExchangeType, int> ExchangeConcurrency { get; init; } = [];
    public int ApiPageSize { get; init; } = 1000;
    public int MaxHistoryMonths { get; init; } = 1;
    public int MaxRetries { get; init; } = 3;
    public int RetryDelaySeconds { get; init; } = 30;

    public int GetConcurrency(ExchangeType exchange)
    {
        return ExchangeConcurrency.TryGetValue(exchange, out var concurrency)
            ? concurrency
            : DefaultConcurrencyPerExchange;
    }
}
