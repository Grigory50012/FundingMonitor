using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Queues;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Core.Queues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Application.BackgroundServices;

public class HistoricalCollectionBackgroundService : BackgroundService
{
    private readonly ILogger<HistoricalCollectionBackgroundService> _logger;
    private readonly IOptions<HistoricalDataCollectionOptions> _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _semaphore;
    private readonly IHistoryTaskQueue _taskQueue;

    public HistoricalCollectionBackgroundService(
        IHistoryTaskQueue taskQueue,
        IServiceScopeFactory scopeFactory,
        IOptions<HistoricalDataCollectionOptions> options,
        ILogger<HistoricalCollectionBackgroundService> logger)
    {
        _taskQueue = taskQueue;
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
        _semaphore = new SemaphoreSlim(options.Value.MaxConcurrentTasks);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HistoricalCollectionBackgroundService started");

        var activeTasks = new List<Task>();

        while (!stoppingToken.IsCancellationRequested)
        {
            // Очищаем завершённые задачи
            activeTasks.RemoveAll(t => t.IsCompleted);

            // Используем блокирующий DequeueAsync
            var task = await _taskQueue.DequeueAsync(stoppingToken);

            if (task != null)
            {
                // Есть задача - запускаем с ограничением параллелизма
                activeTasks.Add(ProcessHistoricalCollectionTaskAsync(task, stoppingToken));
            }
            else if (activeTasks.Count == 0)
            {
                // Очередь пуста и нет активных задач - ждём
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            else
            {
                // Очередь пуста, но есть активные задачи - ждём завершения любой
                await Task.WhenAny(activeTasks);
            }
        }

        // Ждём завершения всех активных задач перед остановкой
        if (activeTasks.Count > 0)
            await Task.WhenAll(activeTasks);

        _logger.LogInformation("HistoricalCollectionBackgroundService stopped");
    }

    private async Task ProcessHistoricalCollectionTaskAsync(HistoricalCollectionTask task,
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            using var scope = _scopeFactory.CreateScope();

            var exchangeApiClients = scope.ServiceProvider
                .GetRequiredService<IEnumerable<IExchangeFundingRateClient>>();
            var historyRepo = scope.ServiceProvider
                .GetRequiredService<IHistoricalFundingRateRepository>();

            var client = exchangeApiClients.First(c => c.ExchangeType == task.Exchange);
            var symbol = task.NormalizedSymbol;
            var fromTime = DateTime.UtcNow.AddMonths(-_options.Value.MaxHistoryMonths);

            var rates = await client.GetHistoricalFundingRatesAsync(
                symbol, fromTime, DateTime.UtcNow, _options.Value.ApiPageSize, cancellationToken);

            if (rates.Count > 0)
            {
                await historyRepo.AddRangeAsync(rates, cancellationToken);
                _logger.LogInformation("✅ {Exchange}:{Symbol} collected {Count} rates",
                    task.Exchange, task.NormalizedSymbol, rates.Count);
            }
            else
            {
                _logger.LogWarning("⚠️ {Exchange}:{Symbol} No data", task.Exchange, task.NormalizedSymbol);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("{Exchange}:{Symbol} Task cancelled", task.Exchange, task.NormalizedSymbol);
            throw;
        }
        catch (Exception ex)
        {
            var retryCount = task.RetryCount + 1;

            if (retryCount >= _options.Value.MaxRetries)
            {
                _logger.LogError(ex, "❌ {Exchange}:{Symbol} Task failed after {RetryCount} attempts",
                    task.Exchange, task.NormalizedSymbol, retryCount);
            }
            else
            {
                task.RetryCount = retryCount;
                await _taskQueue.EnqueueAsync(task, cancellationToken);
                _logger.LogWarning(ex,
                    "⚠️ {Exchange}:{Symbol} Task will be retried (attempt {RetryCount}/{MaxRetries})",
                    task.Exchange, task.NormalizedSymbol, retryCount, _options.Value.MaxRetries);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _semaphore.Dispose();
        await base.StopAsync(cancellationToken);
    }
}