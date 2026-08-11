using Microsoft.Extensions.Hosting;

namespace FundingMonitor.Infrastructure.HistoricalRates.Messaging;

public sealed class RabbitMqTopologyHostedService : IHostedService
{
    private readonly RabbitMqTopologyInitializer _initializer;

    public RabbitMqTopologyHostedService(RabbitMqTopologyInitializer initializer)
    {
        _initializer = initializer;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _initializer.InitializeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
