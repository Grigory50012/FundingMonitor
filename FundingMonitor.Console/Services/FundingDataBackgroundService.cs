using FundingMonitor.Application.Interfaces.Repositories;
using FundingMonitor.Application.Interfaces.Services;
using FundingMonitor.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Console.Services;

public class FundingDataBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly IOptions<DataCollectionOptions> _dataCollectionOptions;
    
    public FundingDataBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<FundingDataBackgroundService> logger,
        IOptions<DataCollectionOptions> dataCollectionOptions)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _dataCollectionOptions = dataCollectionOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Funding Data Background Service запущен.");
        
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_dataCollectionOptions.Value.IntervalMinutes));
        
        // Первый запуск сразу после старта
        await ProcessDataCollectionAsync();
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessDataCollectionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла ошибка во время сбора данных.");
            }
        }
    }

    private async Task ProcessDataCollectionAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        _logger.LogInformation("Начало цикла сбора данных");
        
        using var scope = _scopeFactory.CreateScope();
        
        var collector = scope.ServiceProvider.GetRequiredService<IDataCollector>();
        var repository = scope.ServiceProvider.GetRequiredService<IFundingRateRepository>();
        
        try
        {
            // 1. Собираем данные
            using var cts = new CancellationTokenSource(
                TimeSpan.FromSeconds(_dataCollectionOptions.Value.CollectionTimeoutSeconds));
        
            var collectionTask = collector.CollectAllRatesAsync(cts.Token);

            // Ждем завершения или таймаута
            if (await Task.WhenAny(collectionTask, 
                    Task.Delay(TimeSpan.FromSeconds(_dataCollectionOptions.Value.CollectionTimeoutSeconds), 
                        cts.Token)) == collectionTask)
            {
                var allRates = await collectionTask;
                var collectionTime = stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("Собрано {Count} ставок финансирования за {Time}мс",
                    allRates.Count, collectionTime);

                // 2. Сохраняем в БД
                await repository.SaveRatesAsync(allRates);
                var saveTime = stopwatch.ElapsedMilliseconds - collectionTime;
                _logger.LogInformation("Сохранено {Count} ставок в базу данных за {Time}мс",
                        allRates.Count, saveTime);
                
                stopwatch.Stop();
                _logger.LogInformation("Цикл сбора завершен за {TotalTime}мс", stopwatch.ElapsedMilliseconds);
            }
            else
            {
                await cts.CancelAsync();
                throw new TimeoutException("Истекло время ожидания сбора данных в цикле");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка в цикле сбора данных: {exMessage}", ex.Message);
            throw;
        }
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Funding Data Background Service останавливается...");
        await base.StopAsync(cancellationToken);
    }
}