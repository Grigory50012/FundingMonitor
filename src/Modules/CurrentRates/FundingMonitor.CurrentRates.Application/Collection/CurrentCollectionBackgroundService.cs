using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.CurrentRates.Application.Collection;

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
            var started = Stopwatch.GetTimestamp();
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var collector = scope.ServiceProvider.GetRequiredService<ICurrentFundingRateCollector>();
                var result = await collector.CollectAsync(stoppingToken);
                logger.LogInformation(
                    "Current-rate cycle completed: {Rates} rates, {Events} events, {Succeeded} exchanges succeeded, {Failed} failed in {DurationMs} ms",
                    result.RatesCount,
                    result.EventsCount,
                    result.SuccessfulExchanges,
                    result.FailedExchanges, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
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
