using System.Diagnostics;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Events;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Application.Services;

/// <summary>
///     Сервис для сбора текущих ставок финансирования с бирж
/// </summary>
public class CurrentFundingRateCollector : ICurrentFundingRateCollector
{
    private readonly IEnumerable<IExchangeFundingRateClient> _exchangeClients;
    private readonly ILogger<CurrentFundingRateCollector> _logger;
    private readonly IHistoricalCollectionProducer _producer;
    private readonly ICurrentFundingRateRepository _repository;

    public CurrentFundingRateCollector(
        IEnumerable<IExchangeFundingRateClient> exchangeClients,
        ICurrentFundingRateRepository repository,
        IHistoricalCollectionProducer producer,
        ILogger<CurrentFundingRateCollector> logger)
    {
        _exchangeClients = exchangeClients;
        _repository = repository;
        _producer = producer;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<CurrentFundingRate>> CollectFundingRatesAsync(
        CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScope("CollectionCycle:{CycleId}", Guid.NewGuid().ToString("N")[..8]);

        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Collection cycle started");

        var results = await Task.WhenAll(
            _exchangeClients.Select(client => CollectFromExchangeAsync(client, cancellationToken))
        );

        var allRates = results.SelectMany(r => r.Rates).ToList();
        var allEvents = results.SelectMany(r => r.Events).ToList();

        if (allRates.Count != 0)
        {
            await _repository.UpdateAsync(allRates, cancellationToken);
            _logger.LogInformation("Updated {Rates} rates", allRates.Count);
        }

        if (allEvents.Count != 0)
        {
            await _producer.EnqueueHistoricalCollectionTasksAsync(allEvents, cancellationToken);
            _logger.LogInformation("Published {Events} events", allEvents.Count);
        }

        sw.Stop();
        _logger.LogInformation("Collection cycle completed in {Elapsed}ms. Total rates: {Count}",
            sw.ElapsedMilliseconds, allRates.Count);

        return allRates;
    }

    private async Task<(List<CurrentFundingRate> Rates, List<FundingRateEvent> Events)> CollectFromExchangeAsync(
        IExchangeFundingRateClient client, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Получаем новые данные от биржи
            var newRates = await client.GetCurrentFundingRatesAsync(cancellationToken);

            // 2. Читаем предыдущее состояние из БД (ДО обновления)
            var previousRates = await _repository.GetRatesAsync(null, [client.ExchangeType], cancellationToken);
            var previousState = previousRates.ToDictionary(r => r.NormalizedSymbol);

            // 3. Детектируем изменения (новые символы и изменения времени выплаты)
            var events = DetectChanges(client.ExchangeType, previousState, newRates);

            // 4. Возвращаем данные для обновления в БД
            return (newRates, events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting data from {Exchange}", client.ExchangeType);
            return (Rates: [], Events: []);
        }
    }

    /// <summary>
    ///     Детектирует изменения в ставках финансирования
    /// </summary>
    private List<FundingRateEvent> DetectChanges(
        ExchangeType exchange,
        Dictionary<string, CurrentFundingRate> previous,
        List<CurrentFundingRate> current)
    {
        var events = new List<FundingRateEvent>();
        var currentDict = current.ToDictionary(r => r.NormalizedSymbol);

        // 1. Обнаружение новых символов
        foreach (var symbol in currentDict.Keys.Except(previous.Keys))
        {
            var rate = currentDict[symbol];
            events.Add(new NewSymbolFundingEvent
            {
                Exchange = exchange,
                NormalizedSymbol = symbol,
                DetectedAt = DateTime.UtcNow,
                FundingIntervalHours = rate.FundingIntervalHours,
                NextFundingTime = rate.NextFundingTime
            });
            _logger.LogDebug("🔍 New symbol: {Exchange}:{Symbol}", exchange, symbol);
        }

        // 2. Обнаружение изменений времени выплаты
        foreach (var symbol in currentDict.Keys.Intersect(previous.Keys))
        {
            var curr = currentDict[symbol];
            var prev = previous[symbol];

            if (curr.NextFundingTime != prev.NextFundingTime)
            {
                events.Add(new FundingTimeChangeEvent
                {
                    Exchange = exchange,
                    NormalizedSymbol = symbol,
                    OldFundingTime = prev.NextFundingTime ?? DateTime.MinValue,
                    NewFundingTime = curr.NextFundingTime ?? DateTime.MinValue,
                    PreviousCheckTime = prev.LastCheck
                });
                _logger.LogDebug("🕐 Funding time changed: {Exchange}:{Symbol} {Old:HH:mm}->{New:HH:mm}",
                    exchange, symbol, prev.NextFundingTime, curr.NextFundingTime);
            }
        }

        return events;
    }
}