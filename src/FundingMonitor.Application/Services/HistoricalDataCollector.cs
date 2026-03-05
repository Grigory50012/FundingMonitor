using System.Collections.Concurrent;
using System.Diagnostics;
using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Events;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Application.Services;

public class HistoricalDataCollector : IHistoricalDataCollector
{
    private readonly IEnumerable<IExchangeApiClient> _clients;
    private readonly ICurrentFundingRateRepository _currentRepo;
    private readonly SemaphoreSlim _globalSemaphore;
    private readonly IHistoricalFundingRateRepository _historyRepo;
    private readonly ILogger<HistoricalDataCollector> _logger;
    private readonly IOptions<HistoricalDataCollectionOptions> _options;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _symbolLocks;

    public HistoricalDataCollector(
        IEnumerable<IExchangeApiClient> clients,
        IHistoricalFundingRateRepository historyRepo,
        ICurrentFundingRateRepository currentRepo,
        IOptions<HistoricalDataCollectionOptions> options,
        ILogger<HistoricalDataCollector> logger)
    {
        _clients = clients;
        _historyRepo = historyRepo;
        _currentRepo = currentRepo;
        _options = options;
        _logger = logger;
        _globalSemaphore = new SemaphoreSlim(options.Value.BatchSize);
        _symbolLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
    }

    public async Task ProcessEventsAsync(List<FundingEvent> events, CancellationToken cancellationToken)
    {
        if (events.Count == 0) return;

        using var _ = _logger.BeginScope("HistoricalCycle:{CycleId}", Guid.NewGuid().ToString("N").Substring(0, 8));

        _logger.LogInformation("Начало обработки {Count} событий", events.Count);

        var groupedBySymbol = events
            .GroupBy(e => $"{e.Exchange}_{e.NormalizedSymbol}")
            .Select(g => g.First())
            .ToList();

        if (groupedBySymbol.Count < events.Count)
            _logger.LogInformation("Уникальных символов для обработки: {UniqueCount}", groupedBySymbol.Count);

        var tasks = new List<Task>();
        var stopwatch = Stopwatch.StartNew();

        foreach (var @event in groupedBySymbol)
            tasks.Add(@event switch
            {
                NewSymbolDetectedEvent newSymbol => ProcessWithSymbolLockAsync(
                    newSymbol, ProcessNewSymbolAsync, cancellationToken),

                FundingTimeChangedEvent timeChanged => ProcessWithSymbolLockAsync(
                    timeChanged, ProcessFundingTimeChangeAsync, cancellationToken),

                _ => Task.CompletedTask
            });

        await Task.WhenAll(tasks);

        stopwatch.Stop();
        _logger.LogInformation("Обработка {Count} событий завершена за {Elapsed}мс",
            groupedBySymbol.Count, stopwatch.ElapsedMilliseconds);
    }

    private async Task ProcessWithSymbolLockAsync<T>(
        T @event,
        Func<T, CancellationToken, Task> processor,
        CancellationToken cancellationToken) where T : FundingEvent
    {
        var lockKey = $"{@event.Exchange}_{@event.NormalizedSymbol}";
        var symbolLock = _symbolLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

        await symbolLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogTrace("Блокировка получена для {LockKey}", lockKey);
            await processor(@event, cancellationToken);
        }
        finally
        {
            symbolLock.Release();
            _logger.LogTrace("Блокировка освобождена для {LockKey}", lockKey);

            if (symbolLock.CurrentCount == 1 && _symbolLocks.TryRemove(lockKey, out var removed))
            {
                removed.Dispose();
                _logger.LogTrace("Блокировка удалена для {LockKey}", lockKey);
            }
        }
    }

    private async Task ProcessNewSymbolAsync(NewSymbolDetectedEvent @event, CancellationToken cancellationToken)
    {
        await _globalSemaphore.WaitAsync(cancellationToken);
        var sw = Stopwatch.StartNew();
        var apiSymbol = @event.NormalizedSymbol.Replace("-", "");

        try
        {
            _logger.LogInformation("🆕 Загрузка истории для нового символа {Exchange}:{Symbol}",
                @event.Exchange, @event.NormalizedSymbol);

            var client = GetExchangeClient(@event.Exchange);

            var fromTime = DateTime.UtcNow.AddMonths(-_options.Value.MaxHistoryMonths);
            var toTime = DateTime.UtcNow;

            var rates = await client.GetHistoricalFundingRatesAsync(
                apiSymbol, fromTime, toTime, _options.Value.ApiPageSize, cancellationToken);

            _logger.LogInformation("✅ {Exchange}:{Symbol} собрано {Count} ставок за {Elapsed}мс",
                @event.Exchange, @event.NormalizedSymbol, rates.Count, sw.ElapsedMilliseconds);

            if (rates.Count != 0)
            {
                var beforeSave = sw.ElapsedMilliseconds;

                await _historyRepo.SaveRatesAsync(rates, cancellationToken);

                _logger.LogInformation(
                    "✅ {Exchange}:{Symbol} загружено {Count} ставок за {Elapsed}мс (сохранение: {SaveElapsed}мс)",
                    @event.Exchange, @event.NormalizedSymbol, rates.Count,
                    sw.ElapsedMilliseconds, sw.ElapsedMilliseconds - beforeSave);
            }
            else
            {
                _logger.LogWarning("⚠️ Нет исторических данных для {Exchange}:{Symbol}",
                    @event.Exchange, @event.NormalizedSymbol);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка загрузки истории для {Exchange}:{Symbol}",
                @event.Exchange, @event.NormalizedSymbol);
        }
        finally
        {
            sw.Stop();
            _globalSemaphore.Release();
        }
    }

    private async Task ProcessFundingTimeChangeAsync(FundingTimeChangedEvent @event,
        CancellationToken cancellationToken)
    {
        await _globalSemaphore.WaitAsync(cancellationToken);
        var startTime = DateTime.UtcNow;
        var apiSymbol = @event.NormalizedSymbol.Replace("-", "");

        try
        {
            _logger.LogInformation("🔄 Обновление истории после изменения времени {Exchange}:{Symbol}",
                @event.Exchange, @event.NormalizedSymbol);
            _logger.LogDebug("Время изменено: {Old:HH:mm} -> {New:HH:mm}",
                @event.OldFundingTime, @event.NewFundingTime);

            var lastRate = await _historyRepo.GetLastRateAsync(
                @event.Exchange.ToString(), @event.NormalizedSymbol, cancellationToken);

            if (lastRate == null)
            {
                _logger.LogInformation("⚠️ Нет истории для {Exchange}:{Symbol}, обрабатываем как новый символ",
                    @event.Exchange, @event.NormalizedSymbol);

                var newSymbolEvent = await ConvertToNewSymbolEventAsync(@event, cancellationToken);
                await ProcessNewSymbolAsync(newSymbolEvent, cancellationToken);
                return;
            }

            var client = GetExchangeClient(@event.Exchange);
            var fromTime = lastRate.FundingTime;
            var toTime = DateTime.UtcNow;

            _logger.LogDebug("Запрос недостающих данных {ApiSymbol} с {From:yyyy-MM-dd HH:mm}",
                apiSymbol, fromTime);

            var rates = await client.GetHistoricalFundingRatesAsync(
                apiSymbol, fromTime, toTime, _options.Value.ApiPageSize, cancellationToken);

            if (rates.Count != 0)
            {
                await _historyRepo.SaveRatesAsync(rates, cancellationToken);

                var elapsed = DateTime.UtcNow - startTime;
                _logger.LogInformation("✅ {Exchange}:{Symbol} добавлено {Count} ставок за {Elapsed}мс",
                    @event.Exchange, @event.NormalizedSymbol, rates.Count, elapsed.TotalSeconds);
            }
            else
            {
                _logger.LogWarning("ℹ️ Нет новых данных для {Exchange}:{Symbol}", @event.Exchange,
                    @event.NormalizedSymbol);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка обновления истории для {Exchange}:{Symbol}",
                @event.Exchange, @event.NormalizedSymbol);
        }
        finally
        {
            _globalSemaphore.Release();
        }
    }

    private async Task<NewSymbolDetectedEvent> ConvertToNewSymbolEventAsync(
        FundingTimeChangedEvent @event,
        CancellationToken cancellationToken)
    {
        var currentRates = await _currentRepo.GetRatesAsync(
            @event.NormalizedSymbol,
            new List<ExchangeType> { @event.Exchange },
            cancellationToken);

        var currentRate = currentRates.FirstOrDefault();

        return new NewSymbolDetectedEvent
        {
            Exchange = @event.Exchange,
            NormalizedSymbol = @event.NormalizedSymbol,
            DetectedAt = DateTime.UtcNow,
            FundingIntervalHours = currentRate?.FundingIntervalHours ?? 8,
            NextFundingTime = @event.NewFundingTime
        };
    }

    private IExchangeApiClient GetExchangeClient(ExchangeType exchange)
    {
        return _clients.First(c => c.ExchangeType == exchange);
    }
}