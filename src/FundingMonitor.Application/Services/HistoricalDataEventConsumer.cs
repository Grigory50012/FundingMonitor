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
    private readonly ICurrentFundingRateRepository _currentRepo;
    private readonly IHistoricalFundingRateRepository _historyRepo;
    private readonly ILogger<HistoricalDataEventConsumer> _logger;
    private readonly IOptions<HistoricalDataCollectionOptions> _options;

    private readonly Channel<Func<CancellationToken, Task>> _queue;
    private readonly SemaphoreSlim _semaphore;
    private readonly IServiceProvider _serviceProvider;

    public HistoricalDataEventConsumer(
        IServiceProvider serviceProvider,
        IHistoricalFundingRateRepository historyRepo,
        ICurrentFundingRateRepository currentRepo,
        IOptions<HistoricalDataCollectionOptions> options,
        ILogger<HistoricalDataEventConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _historyRepo = historyRepo;
        _currentRepo = currentRepo;
        _options = options;
        _logger = logger;

        _queue = Channel.CreateBounded<Func<CancellationToken, Task>>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true
            });

        _semaphore = new SemaphoreSlim(options.Value.BatchSize);

        Task.Run(ProcessQueueAsync);
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Shutting down HistoricalDataEventConsumer...");

        _cts.Cancel();
        _queue.Writer.TryComplete();

        try
        {
            await Task.Delay(5000, CancellationToken.None);
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

        await _queue.Writer.WriteAsync(async ct =>
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                await ProcessFundingTimeChangeAsync(@event, ct);
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

        await _queue.Writer.WriteAsync(async ct =>
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                await ProcessNewSymbolAsync(@event, ct);
            }
            finally
            {
                _semaphore.Release();
            }
        }, cancellationToken);
    }

    private async Task ProcessNewSymbolAsync(NewSymbolDetectedEvent @event, CancellationToken cancellationToken)
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
                await _historyRepo.SaveRatesAsync(rates, cancellationToken);
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

    private async Task ProcessFundingTimeChangeAsync(FundingTimeChangedEvent @event,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var lastRate = await _historyRepo.GetLastRateAsync(
                @event.Exchange.ToString(), @event.NormalizedSymbol, cancellationToken);

            if (lastRate == null)
            {
                _logger.LogWarning("Нет истории для {Exchange}:{Symbol}, обрабатываем как новый",
                    @event.Exchange, @event.NormalizedSymbol);
                await ProcessAsNewSymbolAsync(@event, cancellationToken);
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
                await _historyRepo.SaveRatesAsync(rates, cancellationToken);
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

    private async Task ProcessAsNewSymbolAsync(FundingTimeChangedEvent @event, CancellationToken cancellationToken)
    {
        // Получаем интервал из текущих данных
        var currentRates = await _currentRepo.GetRatesAsync(
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

        await ProcessNewSymbolAsync(newEvent, cancellationToken);
    }

    private async Task ProcessQueueAsync()
    {
        var reader = _queue.Reader;

        await foreach (var workItem in reader.ReadAllAsync(_cts.Token))
            try
            {
                await workItem(_cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Queue item failed");
            }
    }

    private IExchangeApiClient GetExchangeClient(ExchangeType exchange)
    {
        using var scope = _serviceProvider.CreateScope();
        var clients = scope.ServiceProvider.GetServices<IExchangeApiClient>();
        return clients.First(c => c.ExchangeType == exchange);
    }
}