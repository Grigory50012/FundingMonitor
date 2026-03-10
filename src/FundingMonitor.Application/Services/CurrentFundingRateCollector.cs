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

public class CurrentFundingRateCollector : ICurrentFundingRateCollector
{
    private readonly IEnumerable<IExchangeFundingRateClient> _exchangeClients;
    private readonly IFundingRateHistoryService _historicalCollector;
    private readonly ILogger<CurrentFundingRateCollector> _logger;
    private readonly ICurrentFundingRateRepository _repository;
    private readonly IStateRepository _stateRepository;

    public CurrentFundingRateCollector(
        IEnumerable<IExchangeFundingRateClient> exchangeClients,
        ICurrentFundingRateRepository repository,
        IFundingRateHistoryService historicalCollector,
        IStateRepository stateRepository,
        ILogger<CurrentFundingRateCollector> logger)
    {
        _exchangeClients = exchangeClients;
        _repository = repository;
        _historicalCollector = historicalCollector;
        _stateRepository = stateRepository;
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
        }

        if (allEvents.Count != 0)
        {
            await _historicalCollector.EnqueueHistoricalCollectionTasksAsync(allEvents, cancellationToken);
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

            var previousState = await _stateRepository.GetExchangeStateAsync(client.ExchangeType);
            var events = DetectChanges(client.ExchangeType, previousState, currentState, rates);

            await _stateRepository.SaveExchangeStateAsync(client.ExchangeType, currentState);

            return (rates, events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting data from {Exchange}", client.ExchangeType);
            return (Rates: [], Events: []);
        }
    }

    private List<FundingRateEvent> DetectChanges(
        ExchangeType exchange,
        Dictionary<string, SymbolState> previous,
        Dictionary<string, SymbolState> current,
        List<CurrentFundingRate> rates)
    {
        var events = new List<FundingRateEvent>();
        var now = DateTime.UtcNow;

        foreach (var symbol in current.Keys.Except(previous.Keys))
        {
            var rate = rates.First(r => r.NormalizedSymbol == symbol);
            events.Add(new NewSymbolFundingEvent
            {
                Exchange = exchange,
                NormalizedSymbol = symbol,
                DetectedAt = now,
                FundingIntervalHours = rate.FundingIntervalHours,
                NextFundingTime = rate.NextFundingTime
            });
            _logger.LogDebug("🔍 New symbol: {Exchange}:{Symbol}", exchange, symbol);
        }

        foreach (var symbol in current.Keys.Intersect(previous.Keys))
        {
            var curr = current[symbol];
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