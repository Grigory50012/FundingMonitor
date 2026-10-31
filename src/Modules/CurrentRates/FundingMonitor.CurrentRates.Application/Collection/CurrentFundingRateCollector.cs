using FundingMonitor.CurrentRates.Application.Persistence;
using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates.Domain;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.CurrentRates.Application.Collection;

public sealed class CurrentFundingRateCollector(
    IExchangeMarketDataModule exchangeMarketData,
    ICurrentRatesRepository repository,
    ICurrentRatesUnitOfWork unitOfWork,
    IFundingRateChangeDetector changeDetector,
    TimeProvider timeProvider,
    ILogger<CurrentFundingRateCollector> logger) : ICurrentFundingRateCollector
{
    private static readonly ExchangeId[] Exchanges =
    [
        ExchangeId.Parse("Binance"),
        ExchangeId.Parse("Bybit"),
        ExchangeId.Parse("OKX")
    ];

    public async Task<CurrentCollectionResult> CollectAsync(CancellationToken cancellationToken)
    {
        var previous = await repository.GetAllRatesAsync(cancellationToken);
        var previousByExchange = previous
            .GroupBy(rate => rate.Exchange)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyDictionary<Symbol, CurrentFundingRate>)group
                    .ToDictionary(rate => rate.Symbol));
        var occurredOnUtc = timeProvider.GetUtcNow();

        var results = await Task.WhenAll(Exchanges.Select(exchange =>
            CollectExchangeAsync(exchange, previousByExchange, occurredOnUtc, cancellationToken)));

        var successful = results.Where(result => result.Succeeded).ToArray();
        foreach (var result in successful)
        {
            await unitOfWork.StoreSnapshotAsync(
                result.Exchange,
                result.Rates,
                result.Events,
                cancellationToken);
        }

        return new CurrentCollectionResult(
            true,
            successful.Sum(result => result.Rates.Count),
            successful.Sum(result => result.Events.Count),
            successful.Length,
            results.Length - successful.Length);
    }

    private async Task<ExchangeCollectionResult> CollectExchangeAsync(
        ExchangeId exchange,
        IReadOnlyDictionary<ExchangeId, IReadOnlyDictionary<Symbol, CurrentFundingRate>> previousByExchange,
        DateTimeOffset occurredOnUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await exchangeMarketData.GetCurrentRatesAsync(exchange, cancellationToken);
            if (!response.IsSuccess || response.Value is null)
            {
                logger.LogWarning(
                    "[{Exchange}] Current-rate collection failed: {ErrorCode}",
                    exchange,
                    response.Error?.Code);
                return ExchangeCollectionResult.Failure(exchange);
            }

            var rates = response.Value.Select(Map).ToArray();
            var previous = previousByExchange.GetValueOrDefault(exchange)
                           ?? new Dictionary<Symbol, CurrentFundingRate>();
            var events = changeDetector.DetectChanges(exchange, previous, rates, occurredOnUtc);
            return new ExchangeCollectionResult(exchange, true, rates, events);
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
            FundingRate = snapshot.FundingRate,
            FundingIntervalHours = snapshot.FundingIntervalHours,
            NextFundingTime = snapshot.NextFundingTime,
            PredictedNextRate = snapshot.PredictedNextRate,
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
