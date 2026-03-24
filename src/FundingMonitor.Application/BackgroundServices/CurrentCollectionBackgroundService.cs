using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Application.BackgroundServices;

public class CurrentCollectionBackgroundService : BackgroundService
{
    private readonly ILogger<CurrentCollectionBackgroundService> _logger;
    private readonly IOptions<CurrentDataCollectionOptions> _options;
    private readonly IServiceScopeFactory _scopeFactory;

    public CurrentCollectionBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<CurrentCollectionBackgroundService> logger,
        IOptions<CurrentDataCollectionOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CurrentCollectionBackgroundService started");

        // Небольшая задержка при старте
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.Value.UpdateIntervalSeconds));

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
            await collector.CollectFundingRatesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Collection cycle failed");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CurrentCollectionBackgroundService stopping...");
        await base.StopAsync(cancellationToken);
    }
}