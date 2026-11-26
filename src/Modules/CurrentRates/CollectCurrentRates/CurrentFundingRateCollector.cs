using System.Diagnostics;
using FundingMonitor.CurrentRates.Persistence;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.CurrentRates.CollectCurrentRates;

internal sealed class CurrentFundingRateCollector(
    IExchangeMarketDataModule exchangeMarketData,
    ICurrentRatesRepository repository,
    ILogger<CurrentFundingRateCollector> logger)
{
    public async Task CollectAsync(ExchangeId exchange, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var response = await exchangeMarketData.GetCurrentRatesAsync(exchange, cancellationToken);
        if (!response.IsSuccess || response.Value is null)
        {
            logger.LogDebug("[{Exchange}] Current-rate collection failed: {ErrorCode}",
                exchange.Value, response.Error?.Code);
            return;
        }

        var observedRates = response.Value.Select(Map).ToArray();
        var eventCount = await repository.StoreSnapshotAsync(exchange, observedRates, cancellationToken);
        logger.LogInformation(
            "[{Exchange}] Current-rate cycle completed: {Rates} rates, {Events} events in {DurationMs} ms",
            exchange.Value, observedRates.Length, eventCount,
            Stopwatch.GetElapsedTime(started).TotalMilliseconds);
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
}
