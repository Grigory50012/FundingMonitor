using FundingMonitor.Core.Interfaces;
using FundingMonitor.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Console;

public class FundingDataBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FundingDataBackgroundService> _logger;
    private readonly PeriodicTimer _timer;

    public FundingDataBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<FundingDataBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _timer = new PeriodicTimer(TimeSpan.FromMinutes(1)); // Интервал 1 минута
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Funding Data Background Service started.");
        
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessDataCollectionAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during data collection");
                // Не прерываем выполнение при ошибке, ждем следующей итерации
            }
        }
    }

    private async Task ProcessDataCollectionAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        
        var dataService = scope.ServiceProvider.GetRequiredService<IFundingDataService>();
        var repository = scope.ServiceProvider.GetRequiredService<IFundingRateRepository>();
        
        _logger.LogInformation("Starting data collection cycle...");

        // 1. Собираем данные
        var allRates = await dataService.CollectAllRatesAsync();
        _logger.LogInformation("Collected {Count} funding rates", allRates.Count);

        // 2. Сохраняем в БД
        if (allRates.Any())
        {
            await repository.SaveRatesAsync(allRates);
            _logger.LogInformation("Saved {Count} rates to database", allRates.Count);
        }

        _logger.LogInformation("Data collection cycle completed");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Funding Data Background Service is stopping...");
        _timer.Dispose();
        await base.StopAsync(cancellationToken);
    }
}