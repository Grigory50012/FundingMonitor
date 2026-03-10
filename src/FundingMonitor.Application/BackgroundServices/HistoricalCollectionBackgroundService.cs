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
    private readonly IEnumerable<IExchangeApiClient> _clients;
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly IHistoricalFundingRateRepository _historyRepo;
    private readonly ILogger<HistoricalCollectionBackgroundService> _logger;
    private readonly IOptions<HistoricalCollectionOptions> _options;
    private readonly IHistoricalCollectionQueue _queue;

    public HistoricalCollectionBackgroundService(
        IHistoricalCollectionQueue queue,
        IEnumerable<IExchangeApiClient> clients,
        IHistoricalFundingRateRepository historyRepo,
        IOptions<HistoricalCollectionOptions> options,
        ILogger<HistoricalCollectionBackgroundService> logger)
    {
        _queue = queue;
        _clients = clients;
        _historyRepo = historyRepo;
        _options = options;
        _logger = logger;
        _concurrencySemaphore = new SemaphoreSlim(options.Value.MaxConcurrentTasks);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HistoricalCollectionBackgroundService запущен");

        while (!stoppingToken.IsCancellationRequested)
            try
            {
                if (_queue.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                _logger.LogDebug("Обработка очереди: Pending={Count}", _queue.Count);

                var tasks = new List<Task>();

                // Забираем пакет задач
                for (var i = 0; i < _options.Value.BatchSize && _queue.Count > 0; i++)
                    if (_queue.TryDequeue(out var task))
                        tasks.Add(ProcessTaskAsync(task, stoppingToken));

                if (tasks.Count > 0) await Task.WhenAll(tasks);
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

    private async Task ProcessTaskAsync(HistoricalCollectionTask task, CancellationToken cancellationToken)
    {
        await _concurrencySemaphore.WaitAsync(cancellationToken);
        try
        {
            var sw = Stopwatch.StartNew();

            var client = _clients.First(c => c.ExchangeType == task.Exchange);
            var apiSymbol = task.NormalizedSymbol.Replace("-", "");

            var fromTime = DateTime.UtcNow.AddMonths(-_options.Value.MaxHistoryMonths);
            var toTime = DateTime.UtcNow;

            // Добавляем таймаут на API запрос
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                new CancellationTokenSource(
                    TimeSpan.FromSeconds(_options.Value.RequestTimeoutSeconds)).Token
            );

            var rates = await client.GetHistoricalFundingRatesAsync(
                apiSymbol, fromTime, toTime, _options.Value.ApiPageSize, cts.Token);

            if (rates.Count > 0)
            {
                await _historyRepo.SaveRatesAsync(rates, cancellationToken);

                _logger.LogInformation("✅ {Exchange}:{Symbol} собрано {Count} ставок за {Elapsed}мс",
                    task.Exchange, task.NormalizedSymbol, rates.Count, sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning("⚠️ Нет данных: {Exchange}:{Symbol}",
                    task.Exchange, task.NormalizedSymbol);
            }
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Нормальная отмена
        }
        catch (Exception ex)
        {
            var retryCount = task.RetryCount + 1;

            if (retryCount >= _options.Value.MaxRetries)
            {
                _logger.LogError(ex, "❌ Задача провалена после {RetryCount} попыток: {Exchange}:{Symbol}",
                    retryCount, task.Exchange, task.NormalizedSymbol);
            }
            else
            {
                task.RetryCount = retryCount;
                _queue.Enqueue(task); // Возвращаем в очередь
                _logger.LogWarning(ex,
                    "⚠️ Задача будет повторена (попытка {RetryCount}/{MaxRetries}): {Exchange}:{Symbol}",
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
        _logger.LogInformation("HistoricalCollectionBackgroundService останавливается... Очередь: {Count}",
            _queue.Count);
        _concurrencySemaphore.Dispose();
        await base.StopAsync(cancellationToken);
    }
}