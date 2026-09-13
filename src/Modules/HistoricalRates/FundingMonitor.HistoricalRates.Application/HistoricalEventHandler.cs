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
    public async Task HandleAsync(
        HistoricalCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var result = await exchangeMarketData.GetHistoricalRatesAsync(
            new HistoricalRatesRequest(
                request.Exchange,
                request.Symbol,
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

        await unitOfWork.StoreHistoryAsync(
            rates,
            cancellationToken);
    }
}
