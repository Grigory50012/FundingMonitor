using FundingMonitor.SharedKernel;
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
    private static readonly ExchangeId[] Exchanges =
    [
        ExchangeId.Parse("Binance"),
        ExchangeId.Parse("Bybit"),
        ExchangeId.Parse("OKX")
    ];

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(Exchanges.Select(exchange => RunExchangeAsync(exchange, stoppingToken)));

    private async Task RunExchangeAsync(ExchangeId exchange, CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(
            Math.Max(1, options.Value.UpdateIntervalSeconds)));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var logScope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["CycleId"] = Guid.NewGuid().ToString("N"),
                ["Exchange"] = exchange.Value
            });
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var collector = scope.ServiceProvider.GetRequiredService<CurrentFundingRateCollector>();
                await collector.CollectAsync(exchange, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "[{Exchange}] Current-rate collection cycle failed", exchange.Value);
            }
        }
    }
}
