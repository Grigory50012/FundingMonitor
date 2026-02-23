using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Console.Services;

public class CurrentDataBackgroundService : BackgroundService
{
    private readonly ILogger<CurrentDataBackgroundService> _logger;
    private readonly IOptions<CurrentDataCollectionOptions> _options;
    private readonly IServiceScopeFactory _scopeFactory;

    public CurrentDataBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<CurrentDataBackgroundService> logger,
        IOptions<CurrentDataCollectionOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Current Data Collector запущен");

        // Небольшая задержка при старте
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.Value.UpdateIntervalMinutes));

        // Первый запуск сразу
        await DoCollectionAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await DoCollectionAsync(stoppingToken);
        }
    }

    private async Task DoCollectionAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var collector = scope.ServiceProvider.GetRequiredService<ICurrentDataCollector>();

        try
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Начало цикла сбора");

            var rates = await collector.CollectAsync(stoppingToken);

            _logger.LogInformation("Цикл сбора завершен: {Count} ставок, {Elapsed:F1}s",
                rates.Count, (DateTime.UtcNow - startTime).TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Цикл сбора не выполнен");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Funding Data Background Service останавливается...");
        await base.StopAsync(cancellationToken);
    }
}