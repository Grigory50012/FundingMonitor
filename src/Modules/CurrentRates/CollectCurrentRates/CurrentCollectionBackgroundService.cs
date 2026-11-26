using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.CurrentRates.CollectCurrentRates;

internal sealed class CurrentCollectionBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<CurrentDataCollectionOptions> options,
    ILogger<CurrentCollectionBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(
            Math.Max(1, options.Value.UpdateIntervalSeconds)));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var logScope = logger.BeginScope(new Dictionary<string, object?> { ["CycleId"] = Guid.NewGuid().ToString("N") });
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var collector = scope.ServiceProvider.GetRequiredService<CurrentFundingRateCollector>();
                await collector.CollectAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Current-rate collection cycle failed");
            }
        }
    }
}
