using FundingMonitor.Core.Entities;

namespace FundingMonitor.Infrastructure.Messaging.RabbitMq;

public static class RabbitMqTopology
{
    public const string ExchangeName = "funding.history";
    public const string DeadQueueName = "history.dead";
    public const string DeadRoutingKey = "dead";

    public static string GetQueueName(ExchangeType exchange)
    {
        return exchange switch
        {
            ExchangeType.Binance => "history.binance",
            ExchangeType.Bybit => "history.bybit",
            ExchangeType.OKX => "history.okx",
            _ => throw new ArgumentOutOfRangeException(nameof(exchange), exchange, null)
        };
    }

    public static string GetRoutingKey(ExchangeType exchange)
    {
        return exchange switch
        {
            ExchangeType.Binance => "binance",
            ExchangeType.Bybit => "bybit",
            ExchangeType.OKX => "okx",
            _ => throw new ArgumentOutOfRangeException(nameof(exchange), exchange, null)
        };
    }
}
