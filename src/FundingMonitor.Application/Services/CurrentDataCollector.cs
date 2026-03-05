using System.Diagnostics;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Events;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Core.Interfaces.Services;
using FundingMonitor.Core.Interfaces.State;
using FundingMonitor.Core.State;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Application.Services;

public class CurrentDataCollector : ICurrentDataCollector
{
    private readonly IEnumerable<IExchangeApiClient> _clients;
    private readonly IHistoricalDataCollector _historicalCollector;
    private readonly ILogger<CurrentDataCollector> _logger;
    private readonly ICurrentFundingRateRepository _repository;
    private readonly IStateManager _stateManager;

    public CurrentDataCollector(
        IEnumerable<IExchangeApiClient> clients,
        ICurrentFundingRateRepository repository,
        IHistoricalDataCollector historicalCollector,
        IStateManager stateManager,
        ILogger<CurrentDataCollector> logger)
    {
        _clients = clients;
        _repository = repository;
        _historicalCollector = historicalCollector;
        _stateManager = stateManager;
        _logger = logger;
    }

    public async Task<List<CurrentFundingRate>> CollectAsync(CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScope("CollectionCycle:{CycleId}", Guid.NewGuid().ToString("N").Substring(0, 8));

        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Начало цикла сбора данных");

        // Запускаем сбор данных со всех клиентов параллельно
        var collectTasks = _clients.Select(client => CollectFromExchangeAsync(client, cancellationToken));
        var results = await Task.WhenAll(collectTasks);

        // Объединяем результаты
        var allRates = results.SelectMany(r => r.Rates).ToList();
        var allEvents = results.SelectMany(r => r.Events).ToList();

        // Сохраняем текущие данные
        if (allRates.Count != 0)
        {
            await _repository.UpdateRatesAsync(allRates, cancellationToken);
            _logger.LogInformation("Сохранено {Count} текущих ставок", allRates.Count);
        }

        // Обрабатываем события для исторических данных
        if (allEvents.Count != 0)
        {
            _logger.LogInformation("Обнаружено {Count} изменений", allEvents.Count);

            // Группируем по типу для статистики
            var newSymbols = allEvents.OfType<NewSymbolDetectedEvent>().Count();
            var timeChanges = allEvents.OfType<FundingTimeChangedEvent>().Count();

            if (newSymbols > 0)
                _logger.LogInformation("Новые символы: {Count}", newSymbols);
            if (timeChanges > 0)
                _logger.LogInformation("Изменения времени: {Count}", timeChanges);

            await _historicalCollector.ProcessEventsAsync(allEvents, cancellationToken);
        }

        sw.Stop();
        _logger.LogInformation("Цикл сбора завершен за {Elapsed}мс. Всего ставок: {Count}",
            sw.ElapsedMilliseconds, allRates.Count);

        return allRates;
    }

    private async Task<(List<CurrentFundingRate> Rates, List<FundingEvent> Events)> CollectFromExchangeAsync(
        IExchangeApiClient client,
        CancellationToken cancellationToken)
    {
        try
        {
            var rates = await client.GetCurrentFundingRatesAsync(cancellationToken);
            var currentState = rates.ToDictionary(
                r => r.NormalizedSymbol,
                r => new SymbolState
                {
                    NormalizedSymbol = r.NormalizedSymbol,
                    NextFundingTime = r.NextFundingTime,
                    FundingRate = r.FundingRate,
                    FundingIntervalHours = r.FundingIntervalHours,
                    LastCheck = r.LastCheck,
                    IsActive = r.IsActive
                });

            var previousState = await _stateManager.GetExchangeStateAsync(client.ExchangeType);
            var events = DetectChanges(client.ExchangeType, previousState, currentState, rates);

            await _stateManager.SaveExchangeStateAsync(client.ExchangeType, currentState);

            return (rates, events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сбора данных с {Exchange}", client.ExchangeType);
            return (new List<CurrentFundingRate>(), new List<FundingEvent>());
        }
    }

    private List<FundingEvent> DetectChanges(
        ExchangeType exchange,
        Dictionary<string, SymbolState> previous,
        Dictionary<string, SymbolState> current,
        List<CurrentFundingRate> rates)
    {
        var events = new List<FundingEvent>();
        var now = DateTime.UtcNow;

        // Новые символы
        foreach (var symbol in current.Keys.Except(previous.Keys))
        {
            var rate = rates.First(r => r.NormalizedSymbol == symbol);
            events.Add(new NewSymbolDetectedEvent
            {
                Exchange = exchange,
                NormalizedSymbol = symbol,
                DetectedAt = now,
                FundingIntervalHours = rate.FundingIntervalHours,
                NextFundingTime = rate.NextFundingTime
            });
            _logger.LogDebug("🔍 Новый символ: {Exchange}:{Symbol}", exchange, symbol);
        }

        // Изменение времени выплаты
        foreach (var symbol in current.Keys.Intersect(previous.Keys))
        {
            var curr = current[symbol];
            var prev = previous[symbol];

            if (curr.NextFundingTime != prev.NextFundingTime)
            {
                events.Add(new FundingTimeChangedEvent
                {
                    Exchange = exchange,
                    NormalizedSymbol = symbol,
                    OldFundingTime = prev.NextFundingTime ?? DateTime.MinValue,
                    NewFundingTime = curr.NextFundingTime ?? DateTime.MinValue,
                    PreviousCheckTime = prev.LastCheck
                });
                _logger.LogDebug("🕐 Изменение времени выплаты: {Exchange}:{Symbol} {Old:HH:mm}->{New:HH:mm}",
                    exchange, symbol, prev.NextFundingTime, curr.NextFundingTime);
            }
        }

        return events;
    }
}