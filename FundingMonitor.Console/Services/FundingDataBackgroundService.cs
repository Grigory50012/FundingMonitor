using FundingMonitor.Application.Interfaces.Repositories;
using FundingMonitor.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Console.Services;

public class FundingDataBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    
    public FundingDataBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<FundingDataBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Funding Data Background Service запущен.");
        
        var intervalMinutes = _configuration.GetValue("DataCollection:IntervalMinutes", 1);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));
        
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
            var timeoutSeconds = _configuration.GetValue("DataCollection:CollectionTimeoutSeconds", 15);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        
            var collectionTask = collector.CollectAllRatesAsync(cts.Token);

            // Ждем завершения или таймаута
            if (await Task.WhenAny(collectionTask, Task.Delay(TimeSpan.FromSeconds(30), cts.Token)) == collectionTask)
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