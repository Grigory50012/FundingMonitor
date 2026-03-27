using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Interfaces.Queues;
using FundingMonitor.Core.Interfaces.Services;
using FundingMonitor.Core.Queues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Application.BackgroundServices;

public class HistoricalCollectionBackgroundService : BackgroundService
{
    private readonly ILogger<HistoricalCollectionBackgroundService> _logger;
    private readonly IHistoryTaskQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _semaphore;

    public HistoricalCollectionBackgroundService(
        IHistoryTaskQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<HistoricalDataCollectionOptions> options,
        ILogger<HistoricalCollectionBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
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

            // Получаем задачу из очереди
            var task = await _queue.DequeueAsync(stoppingToken);

            if (task != null)
            {
                // Запускаем с ограничением параллелизма
                activeTasks.Add(ProcessTaskAsync(task, stoppingToken));
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

    private async Task ProcessTaskAsync(HistoricalCollectionTask task, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var collector = scope.ServiceProvider
                .GetRequiredService<IHistoricalFundingRateCollector>();

            await collector.CollectAndSaveAsync(task, cancellationToken);
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