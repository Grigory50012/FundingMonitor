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

public class FundingRateCollector : ICurrentDataCollector
{
    private readonly IEnumerable<IExchangeApiClient> _exchangeClients;
    private readonly IFundingRateHistoryService _historicalCollector;
    private readonly ILogger<FundingRateCollector> _logger;
    private readonly ICurrentFundingRateRepository _repository;
    private readonly IStateManager _stateManager;

    public FundingRateCollector(
        IEnumerable<IExchangeApiClient> exchangeClients,
        ICurrentFundingRateRepository repository,
        IFundingRateHistoryService historicalCollector,
        IStateManager stateManager,
        ILogger<FundingRateCollector> logger)
    {
        _exchangeClients = exchangeClients;
        _repository = repository;
        _historicalCollector = historicalCollector;
        _stateManager = stateManager;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<CurrentFundingRate>> CollectCurrentRatesAsync(
        CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScope("CollectionCycle:{CycleId}", Guid.NewGuid().ToString("N").Substring(0, 8));

        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Collection cycle started");

        // Запускаем сбор данных со всех клиентов параллельно
        var collectionTasks = _exchangeClients.Select(client =>
            CollectFromExchangeAsync(client, cancellationToken));

        var results = await Task.WhenAll(collectionTasks);

        // Объединяем результаты
        var allRates = results
            .SelectMany(result => result.Rates)
            .ToList()
            .AsReadOnly();

        var allEvents = results.SelectMany(r => r.Events).ToList();

        // Сохраняем текущие данные
        if (allRates.Count != 0)
        {
            await _repository.UpdateRatesAsync(allRates, cancellationToken);
            _logger.LogInformation("Saved {Count} current rates", allRates.Count);
        }

        // Обрабатываем события для исторических данных
        if (allEvents.Count != 0)
        {
            await _historicalCollector.ProcessDetectionEventsAsync(allEvents, cancellationToken);
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