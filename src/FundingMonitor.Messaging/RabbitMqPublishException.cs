namespace FundingMonitor.Messaging;

public sealed class RabbitMqPublishException : Exception
{
    public RabbitMqPublishException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}