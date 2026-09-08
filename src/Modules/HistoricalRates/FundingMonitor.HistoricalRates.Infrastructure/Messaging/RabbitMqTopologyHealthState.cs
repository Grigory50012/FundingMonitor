namespace FundingMonitor.HistoricalRates.Infrastructure.Messaging;

internal sealed class RabbitMqTopologyHealthState
{
    public string? LastError { get; private set; }

    public void Healthy() => LastError = null;
    public void Failed(Exception exception) => LastError = exception.Message;
}
