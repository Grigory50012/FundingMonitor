using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.CurrentRates.Infrastructure.Outbox;

internal sealed class OutboxPublisherBackgroundService(
    OutboxPublisher publisher,
    IOptions<OutboxOptions> options,
    ILogger<OutboxPublisherBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await publisher.ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "CurrentRates Outbox cycle failed");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(Math.Max(1, options.Value.PollIntervalSeconds)),
                stoppingToken);
        }
    }
}
