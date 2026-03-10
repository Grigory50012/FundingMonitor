using System.Diagnostics;
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

    public CurrentDataBackgroundService(IServiceScopeFactory scopeFactory,
        ILogger<CurrentDataBackgroundService> logger,
        IOptions<CurrentDataCollectionOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CurrentDataBackgroundService запущен");

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
        var collector = scope.ServiceProvider.GetRequiredService<ICurrentFundingRateCollector>();

        try
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Начало сбора ставок финансирования");

            var rates = await collector.CollectFundingRatesAsync(stoppingToken);

            sw.Stop();
            _logger.LogInformation("Цикл сбора ставок финансирования завершен: {Count} ставок, {Elapsed}мс",
                rates.Count, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Цикл сбора ставок финансирования не выполнен");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CurrentDataBackgroundService останавливается...");
        await base.StopAsync(cancellationToken);
    }
}