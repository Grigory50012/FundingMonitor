
namespace FundingMonitor.Core.HistoricalRates.Messaging;

/// <summary>
/// Устойчивая задача на сбор исторических ставок финансирования.
/// </summary>
public sealed record HistoricalCollectionTask(
    Guid TaskId,
    ExchangeType Exchange,
    string NormalizedSymbol,
    DateTime CreatedAt,
    int RetryCount = 0);
