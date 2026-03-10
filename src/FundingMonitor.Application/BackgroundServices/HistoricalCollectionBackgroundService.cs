using System.Diagnostics;
using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Queues;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Core.Queues;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Application.BackgroundServices;

public class HistoricalCollectionBackgroundService : BackgroundService
{
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly IEnumerable<IExchangeFundingRateClient> _exchangeApiClients;
    private readonly IHistoricalFundingRateRepository _historyRepo;
    private readonly ILogger<HistoricalCollectionBackgroundService> _logger;
    private readonly IOptions<HistoricalDataCollectionOptions> _options;
    private readonly IHistoricalCollectionTaskQueue _taskQueue;

    public HistoricalCollectionBackgroundService(
        IHistoricalCollectionTaskQueue taskQueue,
        IEnumerable<IExchangeFundingRateClient> exchangeApiClients,
        IHistoricalFundingRateRepository historyRepo,
        IOptions<HistoricalDataCollectionOptions> options,
        ILogger<HistoricalCollectionBackgroundService> logger)
    {
        _taskQueue = taskQueue;
        _exchangeApiClients = exchangeApiClients;
        _historyRepo = historyRepo;
        _options = options;
        _logger = logger;
        _concurrencySemaphore = new SemaphoreSlim(options.Value.MaxConcurrentTasks);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HistoricalCollectionBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_taskQueue.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                _logger.LogDebug("Обработка очереди: Pending={Count}", _taskQueue.Count);

                var tasks = new List<Task>();

                for (var i = 0; i < _options.Value.BatchSize && _taskQueue.Count > 0; i++)
                    if (_taskQueue.TryDequeue(out var task))
                        tasks.Add(ProcessHistoricalCollectionTaskAsync(task, stoppingToken));

                if (tasks.Count > 0)
                    await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("HistoricalCollectionBackgroundService остановлен");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в цикле обработки очереди");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessHistoricalCollectionTaskAsync(HistoricalCollectionTask task,
        CancellationToken cancellationToken)
    {
        await _concurrencySemaphore.WaitAsync(cancellationToken);
        var sw = Stopwatch.StartNew();

        try
        {
            var client = _exchangeApiClients.First(c => c.ExchangeType == task.Exchange);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                new CancellationTokenSource(
                    TimeSpan.FromSeconds(_options.Value.RequestTimeoutSeconds)).Token
            );

            var fromTime = DateTime.UtcNow.AddMonths(-_options.Value.MaxHistoryMonths);
            var toTime = DateTime.UtcNow;

            var rates = await client.GetHistoricalFundingRatesAsync(
                task.NormalizedSymbol.Replace("-", ""), fromTime, toTime,
                _options.Value.ApiPageSize, cts.Token);

            if (rates.Count > 0)
            {
                await _historyRepo.SaveAsync(rates, cancellationToken);
                _logger.LogInformation("✅ {Exchange}:{Symbol} collected {Count} rates in {Elapsed}ms",
                    task.Exchange, task.NormalizedSymbol, rates.Count, sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning("⚠️ No data: {Exchange}:{Symbol}", task.Exchange, task.NormalizedSymbol);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Task cancelled: {Exchange}:{Symbol}", task.Exchange, task.NormalizedSymbol);
            throw;
        }
        catch (Exception ex)
        {
            var retryCount = task.RetryCount + 1;

            if (retryCount >= _options.Value.MaxRetries)
            {
                _logger.LogError(ex, "❌ Task failed after {RetryCount} attempts: {Exchange}:{Symbol}",
                    retryCount, task.Exchange, task.NormalizedSymbol);
            }
            else
            {
                task.RetryCount = retryCount;
                _taskQueue.Enqueue(task);
                _logger.LogWarning(ex,
                    "⚠️ Task will be retried (attempt {RetryCount}/{MaxRetries}): {Exchange}:{Symbol}",
                    retryCount, _options.Value.MaxRetries, task.Exchange, task.NormalizedSymbol);
            }
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("HistoricalCollectionBackgroundService stopping... Queue: {Count}", _taskQueue.Count);
        _concurrencySemaphore.Dispose();
        await base.StopAsync(cancellationToken);
    }
}