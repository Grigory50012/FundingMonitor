using Microsoft.Extensions.Diagnostics.HealthChecks;
using FundingMonitor.Messaging;
using FundingMonitor.SharedKernel;

namespace FundingMonitor.HistoricalRates.Infrastructure.Messaging;

internal sealed class RabbitMqTopologyHealthCheck(
    RabbitMqConnection connection) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            var transport = await connection.GetAsync(timeout.Token);
            await using var channel = await transport.CreateChannelAsync(cancellationToken: timeout.Token);
            await channel.ExchangeDeclarePassiveAsync(RabbitMqTopology.ExchangeName, timeout.Token);
            foreach (var exchange in new[] { "Binance", "Bybit", "OKX" })
                await channel.QueueDeclarePassiveAsync(
                    RabbitMqTopology.GetQueueName(ExchangeId.Parse(exchange)), timeout.Token);
            await channel.QueueDeclarePassiveAsync(RabbitMqTopology.DeadQueueName, timeout.Token);
            return HealthCheckResult.Healthy("RabbitMQ transport and topology are available");
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded("RabbitMQ transport or topology is unavailable", exception);
        }
    }
}
