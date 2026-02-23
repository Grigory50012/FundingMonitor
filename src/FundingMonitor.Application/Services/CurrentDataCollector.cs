using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Events;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Events;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Core.Interfaces.Services;
using FundingMonitor.Core.Interfaces.State;
using FundingMonitor.Core.State;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Application.Services;

public class CurrentDataCollector : ICurrentDataCollector
{
    private readonly IEnumerable<IExchangeApiClient> _clients;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CurrentDataCollector> _logger;
    private readonly ICurrentFundingRateRepository _repository;
    private readonly IStateManager _stateManager;

    public CurrentDataCollector(
        IEnumerable<IExchangeApiClient> clients,
        ICurrentFundingRateRepository repository,
        IEventPublisher eventPublisher,
        IStateManager stateManager,
        ILogger<CurrentDataCollector> logger)
    {
        _clients = clients;
        _repository = repository;
        _eventPublisher = eventPublisher;
        _stateManager = stateManager;
        _logger = logger;
    }

    public async Task<List<CurrentFundingRate>> CollectAsync(CancellationToken cancellationToken)
    {
        // Запускаем сбор данных со всех клиентов параллельно
        var collectTasks = _clients.Select(client => CollectFromExchangeAsync(client, cancellationToken));
        var results = await Task.WhenAll(collectTasks);

        // Объединяем результаты
        var allRates = results.SelectMany(r => r.Rates).ToList();
        var allEvents = results.SelectMany(r => r.Events).ToList();

        // Сохраняем данные и публикуем события
        if (allRates.Count != 0) await _repository.UpdateRatesAsync(allRates, cancellationToken);

        if (allEvents.Count != 0)
        {
            await _eventPublisher.PublishBatchAsync(allEvents, cancellationToken);
            _logger.LogInformation("Опубликовано {Count} событий", allEvents.Count);
        }

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
            _logger.LogError(ex, "Не удалось получить данные от {Exchange}", client.ExchangeType);
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
            _logger.LogInformation("Новый символ: {Exchange}:{Symbol}", exchange, symbol);
        }

        // Удаленные символы
        foreach (var symbol in previous.Keys.Except(current.Keys))
        {
            events.Add(new SymbolRemovedEvent
            {
                Exchange = exchange,
                NormalizedSymbol = symbol,
                RemovedAt = now
            });
            _logger.LogInformation("Символ удален: {Exchange}:{Symbol}", exchange, symbol);
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
                _logger.LogInformation("Время финансирования изменено: {Exchange}:{Symbol} {Old}->{New}",
                    exchange, symbol, prev.NextFundingTime, curr.NextFundingTime);
            }
        }

        return events;
    }
}