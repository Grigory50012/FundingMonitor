namespace FundingMonitor.Infrastructure.Messaging.RabbitMq;

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
