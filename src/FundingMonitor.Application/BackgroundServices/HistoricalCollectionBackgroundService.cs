using System.Diagnostics;
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
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly ILogger<HistoricalCollectionBackgroundService> _logger;
    private readonly IOptions<HistoricalDataCollectionOptions> _options;
    private readonly IServiceScopeFactory _scopeFactory;
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

                _logger.LogDebug("Queue processing: Pending={Count}", _taskQueue.Count);

                var tasks = new List<Task>();

                for (var i = 0; i < _options.Value.BatchSize && _taskQueue.Count > 0; i++)
                {
                    if (_taskQueue.TryDequeue(out var task))
                    {
                        tasks.Add(ProcessHistoricalCollectionTaskAsync(task!, stoppingToken));
                    }
                }

                if (tasks.Count > 0)
                    await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("HistoricalCollectionBackgroundService stopped");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in queue processing loop");
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
            using var scope = _scopeFactory.CreateScope();

            var exchangeApiClients = scope.ServiceProvider
                .GetRequiredService<IEnumerable<IExchangeFundingRateClient>>();
            var historyRepo = scope.ServiceProvider
                .GetRequiredService<IHistoricalFundingRateRepository>();

            var client = exchangeApiClients.First(c => c.ExchangeType == task.Exchange);

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
                await historyRepo.AddRangeAsync(rates, cancellationToken);
                _logger.LogInformation("✅ {Exchange}:{Symbol} collected {Count} rates in {Elapsed}ms",
                    task.Exchange, task.NormalizedSymbol, rates.Count, sw.ElapsedMilliseconds);
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
                _taskQueue.Enqueue(task);
                _logger.LogWarning(ex,
                    "⚠️ {Exchange}:{Symbol} Task will be retried (attempt {RetryCount}/{MaxRetries})",
                    task.Exchange, task.NormalizedSymbol, retryCount, _options.Value.MaxRetries);
            }
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("HistoricalCollectionBackgroundService stopping...");
        _concurrencySemaphore.Dispose();
        await base.StopAsync(cancellationToken);
    }
}