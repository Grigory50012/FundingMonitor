using FundingMonitor.ExchangeIntegration.Application;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.SharedKernel;
using Xunit;

namespace FundingMonitor.ExchangeIntegration.UnitTests;

public sealed class ExchangeMarketDataModuleTests
{
    [Fact]
    public async Task GetCurrentRatesAsync_RegisteredGateway_ReturnsSnapshots()
    {
        var snapshot = new CurrentRateSnapshot(
            ExchangeId.Parse("Binance"), Symbol.Create("BTC-USDT"),
            100m, 99m, 0.001m, 8, DateTimeOffset.UtcNow.AddHours(1),
            null, DateTimeOffset.UtcNow);
        var sut = new ExchangeMarketDataModule([new StubGateway([snapshot])]);

        var result = await sut.GetCurrentRatesAsync(
            ExchangeId.Parse("Binance"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(snapshot, Assert.Single(result.Value!));
    }

    [Fact]
    public async Task GetCurrentRatesAsync_MissingGateway_ReturnsStableFailure()
    {
        var sut = new ExchangeMarketDataModule([]);

        var result = await sut.GetCurrentRatesAsync(
            ExchangeId.Parse("OKX"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("Exchange.NotSupported", result.Error!.Code);
    }

    [Fact]
    public async Task CheckHealthAsync_MultipleGateways_ReturnsStableExchangeOrder()
    {
        var sut = new ExchangeMarketDataModule(
        [
            new StubGateway([], "Bybit"),
            new StubGateway([], "Binance")
        ]);

        var result = await sut.CheckHealthAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["Binance", "Bybit"], result.Select(item => item.Exchange.Value));
    }


    private sealed class StubGateway(
        IReadOnlyList<CurrentRateSnapshot> snapshots,
        string exchange = "Binance") : IExchangeGateway
    {
        public ExchangeId Exchange => ExchangeId.Parse(exchange);

        public Task<Result<IReadOnlyList<CurrentRateSnapshot>>> GetCurrentRatesAsync(
            CancellationToken token) =>
            Task.FromResult(Result<IReadOnlyList<CurrentRateSnapshot>>.Success(snapshots));

        public Task<Result<IReadOnlyList<HistoricalRatePoint>>> GetHistoricalRatesAsync(
            HistoricalRatesRequest request,
            CancellationToken token) =>
            Task.FromResult(Result<IReadOnlyList<HistoricalRatePoint>>.Success([]));

        public Task<ExchangeHealthSnapshot> CheckHealthAsync(CancellationToken token) =>
            Task.FromResult(new ExchangeHealthSnapshot(
                Exchange, ExchangeHealthStatus.Available, null));
    }
}
