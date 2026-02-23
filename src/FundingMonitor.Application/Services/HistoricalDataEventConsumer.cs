using System.Threading.Channels;
using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Events;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Events;
using FundingMonitor.Core.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Application.Services;

public class HistoricalDataEventConsumer :
    IEventSubscriber<NewSymbolDetectedEvent>,
    IEventSubscriber<FundingTimeChangedEvent>,
    IAsyncDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ILogger<HistoricalDataEventConsumer> _logger;
    private readonly IOptions<HistoricalDataCollectionOptions> _options;
    private readonly int _processorCount = 3;
    private readonly List<Task> _processors;

    private readonly
        Channel<Func<IHistoricalFundingRateRepository, ICurrentFundingRateRepository, CancellationToken, Task>> _queue;

    private readonly SemaphoreSlim _semaphore;
    private readonly IServiceProvider _serviceProvider;

    public HistoricalDataEventConsumer(
        IServiceProvider serviceProvider,
        IOptions<HistoricalDataCollectionOptions> options,
        ILogger<HistoricalDataEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;

        // Изменяем тип канала
        _queue = Channel
            .CreateBounded<Func<IHistoricalFundingRateRepository, ICurrentFundingRateRepository, CancellationToken,
                Task>>(
                new BoundedChannelOptions(1000)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = false
                });

        _semaphore = new SemaphoreSlim(options.Value.BatchSize);

        _processors = new List<Task>();

        // Запускаем N процессоров
        for (var i = 0; i < _processorCount; i++)
        {
            var processorId = i;
            _processors.Add(Task.Run(() => ProcessQueueAsync(processorId)));
        }

        _logger.LogInformation("Запущено {Count} параллельных обработчиков очереди", _processors.Count);
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Shutting down HistoricalDataEventConsumer...");

        _cts.Cancel();
        _queue.Writer.TryComplete();

        try
        {
            await Task.WhenAny(Task.WhenAll(_processors), Task.Delay(5000, CancellationToken.None));
        }
        catch
        {
            // Ignore
        }

        _semaphore.Dispose();
        _cts.Dispose();

        _logger.LogInformation("HistoricalDataEventConsumer stopped");
    }

    public async Task HandleAsync(FundingTimeChangedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Изменение времени ожидания в очереди: {Exchange}:{Symbol}",
            @event.Exchange, @event.NormalizedSymbol);

        // Теперь передаем функцию, которая принимает репозитории
        await _queue.Writer.WriteAsync(async (historyRepo, currentRepo, ct) =>
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                await ProcessFundingTimeChangeAsync(@event, historyRepo, currentRepo, ct);
            }
            finally
            {
                _semaphore.Release();
            }
        }, cancellationToken);
    }

    public string SubscriptionName => "historical_collector";

    public async Task HandleAsync(NewSymbolDetectedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Добавление нового символа в очередь: {Exchange}:{Symbol}",
            @event.Exchange, @event.NormalizedSymbol);

        // Теперь передаем функцию, которая принимает репозитории
        await _queue.Writer.WriteAsync(async (historyRepo, _, ct) =>
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                await ProcessNewSymbolAsync(@event, historyRepo, ct);
            }
            finally
            {
                _semaphore.Release();
            }
        }, cancellationToken);
    }

    private async Task ProcessQueueAsync(int processorId)
    {
        var reader = _queue.Reader;
        _logger.LogDebug("Процессор #{ProcessorId} запущен", processorId);

        await foreach (var workItem in reader.ReadAllAsync(_cts.Token))
        {
            // Создаем новый scope для каждой операции
            using var scope = _serviceProvider.CreateScope();

            try
            {
                // Получаем новые экземпляры репозиториев из scope
                var historyRepo = scope.ServiceProvider.GetRequiredService<IHistoricalFundingRateRepository>();
                var currentRepo = scope.ServiceProvider.GetRequiredService<ICurrentFundingRateRepository>();

                // Выполняем работу с новыми репозиториями
                await workItem(historyRepo, currentRepo, _cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Процессор #{ProcessorId} ошибка: {Message}", processorId, ex.Message);
            }
        }

        _logger.LogDebug("Процессор #{ProcessorId} остановлен", processorId);
    }

    private async Task ProcessNewSymbolAsync(
        NewSymbolDetectedEvent @event,
        IHistoricalFundingRateRepository historyRepo,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Обработка нового символа: {Exchange}:{Symbol}",
            @event.Exchange, @event.NormalizedSymbol);

        try
        {
            var client = GetExchangeClient(@event.Exchange);
            var originalSymbol = @event.NormalizedSymbol.Replace("-", "");

            var fromTime = DateTime.UtcNow.AddMonths(-_options.Value.MaxHistoryMonths);
            var toTime = DateTime.UtcNow;

            var rates = await client.GetHistoricalFundingRatesAsync(
                originalSymbol, fromTime, toTime, _options.Value.ApiPageSize, cancellationToken);

            if (rates.Count != 0)
            {
                // Используем переданный репозиторий
                await historyRepo.SaveRatesAsync(rates, cancellationToken);
                _logger.LogInformation("✓ Новый символ {Exchange}:{Symbol} загружено {Count} ставок за {Elapsed:F1}s",
                    @event.Exchange, @event.NormalizedSymbol, rates.Count,
                    (DateTime.UtcNow - startTime).TotalSeconds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось обработать новый символ {Exchange}:{Symbol}",
                @event.Exchange, @event.NormalizedSymbol);
            throw;
        }
    }

    private async Task ProcessFundingTimeChangeAsync(
        FundingTimeChangedEvent @event,
        IHistoricalFundingRateRepository historyRepo,
        ICurrentFundingRateRepository currentRepo,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Используем переданный репозиторий
            var lastRate = await historyRepo.GetLastRateAsync(
                @event.Exchange.ToString(), @event.NormalizedSymbol, cancellationToken);

            if (lastRate == null)
            {
                _logger.LogWarning("Нет истории для {Exchange}:{Symbol}, обрабатываем как новый",
                    @event.Exchange, @event.NormalizedSymbol);
                await ProcessAsNewSymbolAsync(@event, historyRepo, currentRepo, cancellationToken);
                return;
            }

            var client = GetExchangeClient(@event.Exchange);
            var originalSymbol = @event.NormalizedSymbol.Replace("-", "");

            var fromTime = lastRate.FundingTime;
            var toTime = DateTime.UtcNow;

            var rates = await client.GetHistoricalFundingRatesAsync(
                originalSymbol, fromTime, toTime, _options.Value.ApiPageSize, cancellationToken);

            if (rates.Any())
            {
                // Используем переданный репозиторий
                await historyRepo.SaveRatesAsync(rates, cancellationToken);
                _logger.LogDebug("✓ {Exchange}:{Symbol} обновлено {Count} ставок за {Elapsed:F1}s",
                    @event.Exchange, @event.NormalizedSymbol, rates.Count,
                    (DateTime.UtcNow - startTime).TotalSeconds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось обработать изменение за время {Exchange}:{Symbol}",
                @event.Exchange, @event.NormalizedSymbol);
            throw;
        }
    }

    private async Task ProcessAsNewSymbolAsync(
        FundingTimeChangedEvent @event,
        IHistoricalFundingRateRepository historyRepo,
        ICurrentFundingRateRepository currentRepo,
        CancellationToken cancellationToken)
    {
        // Используем переданный репозиторий
        var currentRates = await currentRepo.GetRatesAsync(
            @event.NormalizedSymbol,
            new List<ExchangeType> { @event.Exchange },
            cancellationToken);

        var currentRate = currentRates.FirstOrDefault();

        var newEvent = new NewSymbolDetectedEvent
        {
            Exchange = @event.Exchange,
            NormalizedSymbol = @event.NormalizedSymbol,
            DetectedAt = DateTime.UtcNow,
            FundingIntervalHours = currentRate?.FundingIntervalHours ?? 8,
            NextFundingTime = @event.NewFundingTime
        };

        await ProcessNewSymbolAsync(newEvent, historyRepo, cancellationToken);
    }

    private IExchangeApiClient GetExchangeClient(ExchangeType exchange)
    {
        using var scope = _serviceProvider.CreateScope();
        var clients = scope.ServiceProvider.GetServices<IExchangeApiClient>();
        return clients.First(c => c.ExchangeType == exchange);
    }
}