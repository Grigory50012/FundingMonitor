using Microsoft.Extensions.Hosting;

namespace FundingMonitor.HistoricalRates.Infrastructure.Messaging;

internal sealed class RabbitMqTopologyHostedService(
    RabbitMqTopologyInitializer initializer) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) =>
        initializer.InitializeAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
