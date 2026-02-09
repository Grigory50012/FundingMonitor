using FundingMonitor.Application.Interfaces.Repositories;
using FundingMonitor.Application.Interfaces.Services;
using FundingMonitor.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Console.Services;

public class FundingDataBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly PeriodicTimer _timer;
    private readonly IConfiguration _configuration;
    
    // Статистика
    private int _successfulCycles;
    private int _failedCycles;
    private DateTime _lastSuccessfulRun = DateTime.MinValue;
    private readonly DateTime? _serviceStartTime;

    public FundingDataBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<FundingDataBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
        
        var intervalMinutes = _configuration.GetValue("DataCollection:IntervalMinutes", 1);
        _timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));
        
        _serviceStartTime = DateTime.UtcNow;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Funding Data Background Service запущен. Интервал сбора данных: {Interval} м", 
            _configuration.GetValue("DataCollection:IntervalMinutes", 1));
        
        // Первый запуск сразу после старта
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        await ProcessDataCollectionAsync();
        
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessDataCollectionAsync();
            }
            catch (Exception ex)
            {
                _failedCycles++;
                _logger.LogError(ex, "Error occurred during data collection (Cycle #{Cycle})", 
                    _successfulCycles + _failedCycles);
            }
        }
    }

    private async Task ProcessDataCollectionAsync()
    {
        var cycleNumber = _successfulCycles + _failedCycles + 1;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        _logger.LogInformation("Начало цикла сбора данных #{Cycle}", cycleNumber);
        
        using var scope = _scopeFactory.CreateScope();
        
        var dataService = scope.ServiceProvider.GetRequiredService<IFundingDataService>();
        var repository = scope.ServiceProvider.GetRequiredService<IFundingRateRepository>();
        
        try
        {
            // 1. Собираем данные
            using var cts = new CancellationTokenSource(
                TimeSpan.FromSeconds(_configuration.GetValue("DataCollection:CollectionTimeoutSeconds", 15)));
        
            var collectionTask = dataService.CollectAllRatesAsync(cts.Token);

            // Ждем завершения или таймаута
            if (await Task.WhenAny(collectionTask, Task.Delay(Timeout.Infinite, cts.Token)) == collectionTask)
            {
                var allRates = await collectionTask;
                var collectionTime = stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("Собрано  {Count} ставок финансирования за {Time}мс",
                    allRates.Count, collectionTime);

                // 2. Сохраняем в БД
                if (allRates.Any())
                {
                    await repository.SaveRatesAsync(allRates);
                    var saveTime = stopwatch.ElapsedMilliseconds - collectionTime;

                    _logger.LogInformation("Сохранено {Count} ставок в базу данных за {Time}мс",
                        allRates.Count, saveTime);

                    // 3. Ищем арбитражные возможности (опционально)
                    if (_configuration.GetValue("DataCollection:ScanArbitrage", true))
                    {
                        var opportunities = dataService.FindArbitrageOpportunitiesAsync(allRates);
                        if (opportunities.Any())
                        {
                            _logger.LogInformation("Found {Count} arbitrage opportunities", opportunities.Count);
                            await ProcessArbitrageOpportunities(opportunities, scope.ServiceProvider);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("No rates collected in cycle #{Cycle}", cycleNumber);
                }
                
                _successfulCycles++;
                _lastSuccessfulRun = DateTime.UtcNow;
            
                stopwatch.Stop();

                _logger.LogInformation("Цикл #{Cycle} завершен за {TotalTime}мс", 
                    cycleNumber, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                await cts.CancelAsync();
                throw new TimeoutException("Data collection timed out after 2 minutes");
            }
        }
        catch (TimeoutException ex)
        {
            _failedCycles++;
            _logger.LogError(ex, "Data collection timeout in cycle #{Cycle}", cycleNumber);
            throw;
        }
        catch (Exception ex)
        {
            _failedCycles++;
            _logger.LogError(ex, "Failed data collection cycle #{Cycle}", cycleNumber);
            throw;
        }
        finally
        {
            // Выводим статистику каждые 10 циклов
            if ((_successfulCycles + _failedCycles) % 10 == 0)
            {
                LogStatistics();
            }
        }
    }
    
    private void LogStatistics()
    {
        var uptime = _serviceStartTime.HasValue 
            ? DateTime.UtcNow - _serviceStartTime.Value 
            : TimeSpan.Zero;
        
        var successRate = _successfulCycles + _failedCycles > 0 
            ? (double)_successfulCycles / (_successfulCycles + _failedCycles) * 100 
            : 0;
        
        _logger.LogInformation("""
                               ========== SERVICE STATISTICS ==========
                               Uptime: {Uptime}
                               Total cycles: {TotalCycles}
                               Successful: {Successful}
                               Failed: {Failed}
                               Success rate: {SuccessRate:F1}%
                               Last successful run: {LastRun}
                               ========================================
                               """,
            uptime.ToString(@"dd\.hh\:mm\:ss"),
            _successfulCycles + _failedCycles,
            _successfulCycles,
            _failedCycles,
            successRate,
            _lastSuccessfulRun.ToString("yyyy-MM-dd HH:mm:ss"));
    }
    
    private Task ProcessArbitrageOpportunities(
        List<ArbitrageOpportunity> opportunities, 
        IServiceProvider serviceProvider)
    {
        try
        {
            // Фильтруем значимые возможности
            var significantOpps = opportunities
                .Where(o => o.MaxDifference > 0.001m) // > 0.1% разницы
                .Take(5) // Берем топ-5
                .ToList();
            
            if (significantOpps.Any())
            {
                // Здесь можно добавить сохранение в БД или отправку уведомлений
                var logger = serviceProvider.GetRequiredService<ILogger<FundingDataBackgroundService>>();
                
                foreach (var opp in significantOpps)
                {
                    logger.LogInformation("Significant arbitrage: {Symbol} - Diff: {Diff:P4}, APR: {APR:F2}%", 
                        opp.Symbol, opp.MaxDifference, opp.AnnualYieldPercent);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing arbitrage opportunities");
        }

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Funding Data Background Service is stopping...");
        _timer.Dispose();
        await base.StopAsync(cancellationToken);
    }
}