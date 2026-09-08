using System.Diagnostics.Metrics;
using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.HistoricalRates.Application.Persistence;
using FundingMonitor.HistoricalRates.Domain;
using Microsoft.Extensions.Options;

namespace FundingMonitor.HistoricalRates.Application;

public sealed class HistoricalEventHandler(
    IExchangeMarketDataModule exchangeMarketData,
    IHistoricalRatesUnitOfWork unitOfWork,
    IOptions<HistoricalDataCollectionOptions> options,
    TimeProvider timeProvider) : IHistoricalEventHandler
{
    public const string ConsumerName = "HistoricalRates.FundingRateChanged.v1";

    private static readonly Meter Meter = new("FundingMonitor.HistoricalRates");
    private static readonly Counter<long> DuplicateCounter =
        Meter.CreateCounter<long>("fundingmonitor.inbox.duplicates");

    public async Task HandleAsync(
        FundingRateChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        if (await unitOfWork.HasProcessedAsync(
                integrationEvent.EventId,
                ConsumerName,
                cancellationToken))
        {
            DuplicateCounter.Add(1, new KeyValuePair<string, object?>(
                "exchange", integrationEvent.Exchange.Value));
            return;
        }

        var now = timeProvider.GetUtcNow();
        var result = await exchangeMarketData.GetHistoricalRatesAsync(
            new HistoricalRatesRequest(
                integrationEvent.Exchange,
                integrationEvent.Symbol,
                now.AddMonths(-options.Value.MaxHistoryMonths),
                now,
                options.Value.ApiPageSize),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
            throw new HistoricalExchangeRequestException(result.Error!);

        var rates = result.Value.Select(point => new HistoricalFundingRate(
            point.Exchange,
            point.Symbol,
            point.FundingRate,
            point.FundingTime,
            point.CollectedOnUtc)).ToArray();

        await unitOfWork.StoreHistoryAndMarkProcessedAsync(
            integrationEvent.EventId,
            ConsumerName,
            rates,
            cancellationToken);
    }
}
