using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FundingMonitor.HistoricalRates.Infrastructure.Messaging;

internal sealed class RabbitMqTopologyHealthCheck(
    RabbitMqTopologyHealthState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(state.LastError is null
            ? HealthCheckResult.Healthy("RabbitMQ topology is available")
            : HealthCheckResult.Degraded(state.LastError));
}
