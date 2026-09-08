using FundingMonitor.SharedKernel;

namespace FundingMonitor.HistoricalRates.Infrastructure.Messaging;

public static class RabbitMqTopology
{
    public const string ExchangeName = "current-rates.events";
    public const string DeadQueueName = "historical-rates.dead";
    public const string DeadRoutingKey = "historical-rates.dead";

    public static readonly IReadOnlyList<string> CollectionQueues =
    [
        "historical-rates.collect.binance",
        "historical-rates.collect.bybit",
        "historical-rates.collect.okx"
    ];

    public static readonly IReadOnlyList<string> AllQueues =
        [.. CollectionQueues, DeadQueueName];

    public static string GetQueueName(ExchangeId exchange) =>
        $"historical-rates.collect.{exchange.Value.ToLowerInvariant()}";

    public static string GetRoutingKey(ExchangeId exchange) =>
        $"funding-rate.changed.{exchange.Value.ToLowerInvariant()}";
}
