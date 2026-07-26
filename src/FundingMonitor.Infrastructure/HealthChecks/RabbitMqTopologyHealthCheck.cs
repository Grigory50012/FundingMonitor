using FundingMonitor.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FundingMonitor.Infrastructure.HealthChecks;

public sealed class RabbitMqTopologyHealthCheck : IHealthCheck
{
    private readonly RabbitMqHealthState _state;

    public RabbitMqTopologyHealthCheck(RabbitMqHealthState state)
    {
        _state = state;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_state.LastError is { } error
            ? HealthCheckResult.Degraded(error)
            : HealthCheckResult.Healthy("RabbitMQ topology is available"));
    }
}
