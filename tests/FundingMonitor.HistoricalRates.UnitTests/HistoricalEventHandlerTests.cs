using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.HistoricalRates.Application;
using FundingMonitor.HistoricalRates.Application.Persistence;
using FundingMonitor.HistoricalRates.Domain;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Options;
using Xunit;

namespace FundingMonitor.HistoricalRates.UnitTests;

public sealed class HistoricalEventHandlerTests
{
    [Fact]
    public async Task RepeatedEvent_CollectsHistoryAgain()
    {
        var market = new Market();
        var store = new Store();
        var handler = new HistoricalEventHandler(market, store,
            Options.Create(new HistoricalDataCollectionOptions()), TimeProvider.System);
        var request = new HistoricalCollectionRequest(Guid.NewGuid(), ExchangeId.Parse("Binance"), Symbol.Create("BTC-USDT"));

        await handler.HandleAsync(request, TestContext.Current.CancellationToken);
        await handler.HandleAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(2, market.Calls);
        Assert.Equal(2, store.Writes);
    }

    private sealed class Store : IHistoricalRatesUnitOfWork
    {
        public int Writes { get; private set; }
        public Task StoreHistoryAsync(IReadOnlyCollection<HistoricalFundingRate> rates, CancellationToken cancellationToken)
        {
            Writes++;
            return Task.CompletedTask;
        }
    }

    private sealed class Market : IExchangeMarketDataModule
    {
        public int Calls { get; private set; }
        public Task<Result<IReadOnlyList<HistoricalRatePoint>>> GetHistoricalRatesAsync(HistoricalRatesRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(Result<IReadOnlyList<HistoricalRatePoint>>.Success([]));
        }
        public Task<Result<IReadOnlyList<CurrentRateSnapshot>>> GetCurrentRatesAsync(ExchangeId exchange, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<ExchangeHealthSnapshot>> CheckHealthAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
