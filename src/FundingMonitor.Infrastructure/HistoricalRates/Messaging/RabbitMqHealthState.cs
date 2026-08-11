namespace FundingMonitor.Infrastructure.HistoricalRates.Messaging;

public sealed class RabbitMqHealthState
{
    private string? _lastError;

    public string? LastError => Volatile.Read(ref _lastError);

    public void MarkHealthy()
    {
        Volatile.Write(ref _lastError, null);
    }

    public void MarkDegraded(string error)
    {
        Volatile.Write(ref _lastError, error);
    }
}
