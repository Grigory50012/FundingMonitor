using System.Diagnostics;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Events;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Core.Interfaces.Services;
using FundingMonitor.Core.Results;
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

    public async Task<CurrentCollectionResult> CollectFundingRatesAsync(CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope("CollectionCycle:{CycleId}", Guid.NewGuid().ToString("N")[..8]);

        var sw = Stopwatch.StartNew();
        _logger.LogDebug("Collection cycle started");

        try
        {
            // 1. Сначала получаем все предыдущие данные ОДНИМ запросом
            var allPreviousRates = await _repository.GetRatesAsync(null, null, cancellationToken);
            var ratesByExchange = allPreviousRates
                .GroupBy(r => r.Exchange)
                .ToDictionary(g => g.Key, g => g.ToDictionary(r => r.NormalizedSymbol));

            // 2. Получаем данные о текущих ставках со всех бирж одновременно
            var results = await Task.WhenAll(
                _exchangeClients.Select(client =>
                    CollectFromExchangeAsync(client, ratesByExchange, cancellationToken))
            );

            var allRates = results.SelectMany(r => r.Rates).ToList();
            var allEvents = results.SelectMany(r => r.Events).ToList();

            if (allRates.Count != 0)
            {
                await _repository.UpdateAsync(allRates, cancellationToken);
                _logger.LogDebug("Updated {Rates} rates", allRates.Count);
            }

            if (allEvents.Count != 0)
            {
                await _producer.EnqueueHistoricalCollectionTasksAsync(allEvents, cancellationToken);
                _logger.LogDebug("Published {Events} events", allEvents.Count);
            }

            sw.Stop();
            _logger.LogInformation("Collection cycle completed: {Count} rates, {Events} events, {Elapsed}ms",
                allRates.Count, allEvents.Count, sw.ElapsedMilliseconds);

            return new CurrentCollectionResult(
                true,
                RatesCount: allRates.Count,
                EventsCount: allEvents.Count);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Collection cycle failed after {Elapsed}ms", sw.ElapsedMilliseconds);

            return new CurrentCollectionResult(
                false,
                ex.Message);
        }
    }

    private async Task<(List<CurrentFundingRate> Rates, List<FundingRateChangedEvent> Events)> CollectFromExchangeAsync(
        IExchangeFundingRateClient client,
        Dictionary<ExchangeType, Dictionary<string, CurrentFundingRate>> previousStateByExchange,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("[{Exchange}] Starting collection", client.ExchangeType);

            // 1. Получаем новые данные от биржи
            var newRates = await client.GetCurrentFundingRatesAsync(cancellationToken);

            // 2. Берём предыдущее состояние из предзагруженных данных
            var previousState = previousStateByExchange.TryGetValue(client.ExchangeType, out var state)
                ? state
                : new Dictionary<string, CurrentFundingRate>();

            // 3. Детектируем изменения (новые символы и изменения времени выплаты)
            var events = DetectChanges(client.ExchangeType, previousState, newRates);

            _logger.LogDebug("[{Exchange}] Collection completed: {Count} rates, {Events} events",
                client.ExchangeType, newRates.Count, events.Count);

            // 4. Возвращаем данные для обновления в БД
            return (newRates, events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Exchange}] Collection failed", client.ExchangeType);
            return (Rates: [], Events: []);
        }
    }

    /// <summary>
    ///     Детектирует изменения в ставках финансирования
    /// </summary>
    private List<FundingRateChangedEvent> DetectChanges(
        ExchangeType exchange,
        Dictionary<string, CurrentFundingRate> previous,
        List<CurrentFundingRate> current)
    {
        var events = new List<FundingRateChangedEvent>(current.Count);

        // Проходим по текущим данным и сравниваем с предыдущими
        foreach (var rate in current)
        {
            if (!previous.TryGetValue(rate.NormalizedSymbol, out var prev))
            {
                // Новый символ
                events.Add(new FundingRateChangedEvent
                {
                    Exchange = exchange,
                    NormalizedSymbol = rate.NormalizedSymbol,
                    NextFundingTime = rate.NextFundingTime
                });
                _logger.LogInformation("+ New symbol detected: {Exchange}:{Symbol}", exchange, rate.NormalizedSymbol);
            }
            else if (rate.NextFundingTime != prev.NextFundingTime)
            {
                // Изменение времени выплаты
                events.Add(new FundingRateChangedEvent
                {
                    Exchange = exchange,
                    NormalizedSymbol = rate.NormalizedSymbol,
                    NextFundingTime = rate.NextFundingTime
                });
                _logger.LogInformation("Funding time changed: {Exchange}:{Symbol} {Old:HH:mm}->{New:HH:mm}",
                    exchange, rate.NormalizedSymbol, prev.NextFundingTime, rate.NextFundingTime);
            }
        }

        return events;
    }
}