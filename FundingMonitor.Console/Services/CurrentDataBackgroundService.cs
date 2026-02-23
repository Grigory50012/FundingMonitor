using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Console.Services;

public class CurrentDataBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<CurrentDataBackgroundService> logger,
    IOptions<CurrentDataCollectionOptions> options)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Current Data Collector запущен");

        // Небольшая задержка при старте
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.Value.UpdateIntervalMinutes));

        // Первый запуск сразу
        await DoCollectionAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await DoCollectionAsync(stoppingToken);
        }
    }

    private async Task DoCollectionAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();

        var collector = scope.ServiceProvider.GetRequiredService<ICurrentDataCollector>();

        try
        {
            var startTime = DateTime.UtcNow;
            logger.LogInformation("Начало цикла сбора");

            var rates = await collector.CollectAsync(stoppingToken);

            logger.LogInformation("Цикл сбора завершен: {Count} ставок, {Elapsed:F1}s",
                rates.Count, (DateTime.UtcNow - startTime).TotalSeconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Цикл сбора не выполнен");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Funding Data Background Service останавливается...");
        await base.StopAsync(cancellationToken);
    }
}