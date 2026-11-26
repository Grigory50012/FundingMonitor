using System.Diagnostics;
using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates.Persistence;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.CurrentRates.CollectCurrentRates;

internal sealed class CurrentFundingRateCollector(
    IExchangeMarketDataModule exchangeMarketData,
    ICurrentRatesRepository repository,
    FundingRateChangeDetector changeDetector,
    TimeProvider timeProvider,
    ILogger<CurrentFundingRateCollector> logger)
{
    private static readonly ExchangeId[] Exchanges =
    [
        ExchangeId.Parse("Binance"),
        ExchangeId.Parse("Bybit"),
        ExchangeId.Parse("OKX")
    ];

    public async Task CollectAsync(CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var storedRates = await repository.GetAllRatesAsync(cancellationToken);
        var storedRatesByExchange = storedRates
            .GroupBy(rate => rate.Exchange)
            .ToDictionary(
                group => group.Key, IReadOnlyDictionary<Symbol, CurrentFundingRate> (group) => group
                    .ToDictionary(rate => rate.Symbol));
        var occurredOnUtc = timeProvider.GetUtcNow();

        var exchangeResults = await Task.WhenAll(Exchanges.Select(exchange =>
            CollectExchangeAsync(exchange, storedRatesByExchange, occurredOnUtc, cancellationToken)));

        var successfulExchanges = exchangeResults.Where(result => result.Succeeded).ToArray();
        foreach (var exchangeResult in successfulExchanges)
        {
            await repository.StoreSnapshotAsync(
                exchangeResult.Exchange,
                exchangeResult.Rates,
                exchangeResult.Events,
                cancellationToken);
        }

        logger.LogInformation(
            "Current-rate cycle completed: {Rates} rates, {Events} events, {Succeeded} exchanges succeeded, {Failed} failed in {DurationMs} ms",
            successfulExchanges.Sum(result => result.Rates.Count),
            successfulExchanges.Sum(result => result.Events.Count),
            successfulExchanges.Length,
            exchangeResults.Length - successfulExchanges.Length,
            Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }

    private async Task<ExchangeCollectionResult> CollectExchangeAsync(
        ExchangeId exchange,
        IReadOnlyDictionary<ExchangeId, IReadOnlyDictionary<Symbol, CurrentFundingRate>> storedRatesByExchange,
        DateTimeOffset occurredOnUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await exchangeMarketData.GetCurrentRatesAsync(exchange, cancellationToken);
            if (!response.IsSuccess || response.Value is null)
            {
                logger.LogDebug(
                    "[{Exchange}] Current-rate collection failed: {ErrorCode}",
                    exchange.Value,
                    response.Error?.Code);
                return ExchangeCollectionResult.Failure(exchange);
            }

            var observedRates = response.Value.Select(Map).ToArray();
            var storedRatesBySymbol = storedRatesByExchange.GetValueOrDefault(exchange)
                                      ?? new Dictionary<Symbol, CurrentFundingRate>();
            var fundingEvents = changeDetector.DetectChanges(exchange, storedRatesBySymbol, observedRates, occurredOnUtc);
            return new ExchangeCollectionResult(exchange, true, observedRates, fundingEvents);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "[{Exchange}] Current-rate collection failed; next cycle will retry", exchange);
            return ExchangeCollectionResult.Failure(exchange);
        }
    }

    private static CurrentFundingRate Map(CurrentRateSnapshot snapshot)
    {
        var parts = snapshot.Symbol.Value.Split('-', 2, StringSplitOptions.RemoveEmptyEntries);
        return new CurrentFundingRate
        {
            Exchange = snapshot.Exchange,
            Symbol = snapshot.Symbol,
            BaseAsset = parts[0],
            QuoteAsset = parts[1],
            MarkPrice = snapshot.MarkPrice,
            IndexPrice = snapshot.IndexPrice,
            BidPrice = snapshot.BidPrice,
            AskPrice = snapshot.AskPrice,
            FundingRate = snapshot.FundingRate,
            FundingIntervalHours = snapshot.FundingIntervalHours,
            NextFundingTime = snapshot.NextFundingTime,
            LastSeenOnUtc = snapshot.ObservedOnUtc,
            IsActive = true
        };
    }

    private sealed record ExchangeCollectionResult(
        ExchangeId Exchange,
        bool Succeeded,
        IReadOnlyCollection<CurrentFundingRate> Rates,
        IReadOnlyCollection<FundingRateChangedIntegrationEvent> Events)
    {
        public static ExchangeCollectionResult Failure(ExchangeId exchange) =>
            new(exchange, false, [], []);
    }
}
