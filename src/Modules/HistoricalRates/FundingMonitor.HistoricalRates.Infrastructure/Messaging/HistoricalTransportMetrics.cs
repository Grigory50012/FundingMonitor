using System.Diagnostics.Metrics;
using FundingMonitor.SharedKernel;

namespace FundingMonitor.HistoricalRates.Infrastructure.Messaging;

internal sealed class HistoricalTransportMetrics : IDisposable
{
    private readonly Meter _meter = new("FundingMonitor.HistoricalRates");
    private readonly Counter<long> _deadLettered;
    private readonly Counter<long> _retries;

    public HistoricalTransportMetrics()
    {
        _retries = _meter.CreateCounter<long>("fundingmonitor.rabbitmq.retries");
        _deadLettered = _meter.CreateCounter<long>("fundingmonitor.rabbitmq.dead_lettered");
    }

    public void Retry(ExchangeId exchange) =>
        _retries.Add(1, new KeyValuePair<string, object?>("exchange", exchange.Value));

    public void DeadLetter(ExchangeId exchange) =>
        _deadLettered.Add(1, new KeyValuePair<string, object?>("exchange", exchange.Value));

    public void Dispose() => _meter.Dispose();
}
