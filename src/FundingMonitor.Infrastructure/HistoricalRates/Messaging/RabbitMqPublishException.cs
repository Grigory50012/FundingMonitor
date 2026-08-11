namespace FundingMonitor.Infrastructure.HistoricalRates.Messaging;

public sealed class RabbitMqPublishException : Exception
{
    public RabbitMqPublishException(string message)
        : base(message)
    {
    }

    public RabbitMqPublishException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
