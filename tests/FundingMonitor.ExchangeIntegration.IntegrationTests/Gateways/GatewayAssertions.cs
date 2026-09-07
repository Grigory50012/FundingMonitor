using FundingMonitor.ExchangeIntegration.Application;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.SharedKernel;
using Xunit;

namespace FundingMonitor.ExchangeIntegration.IntegrationTests.Gateways;

internal static class GatewayAssertions
{
    internal static readonly DateTimeOffset ObservedOnUtc =
        new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

    internal static readonly DateTimeOffset FundingTime =
        new(2026, 9, 7, 8, 0, 0, TimeSpan.Zero);

    internal static readonly DateTimeOffset NextFundingTime =
        new(2026, 9, 7, 16, 0, 0, TimeSpan.Zero);

    internal static async Task CurrentMapsAsync(
        IExchangeGateway gateway,
        StubHttpServer server,
        string expectedPath)
    {
        var result = await gateway.GetCurrentRatesAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess, result.Error?.Code);
        var rate = Assert.Single(result.Value!);
        Assert.Equal(gateway.Exchange, rate.Exchange);
        Assert.Equal(Symbol.Create("BTC-USDT"), rate.Symbol);
        Assert.Equal(100m, rate.MarkPrice);
        Assert.Equal(99m, rate.IndexPrice);
        Assert.Equal(0.001m, rate.FundingRate);
        Assert.Equal(8, rate.FundingIntervalHours);
        Assert.Equal(NextFundingTime, rate.NextFundingTime);
        Assert.Null(rate.PredictedNextRate);
        Assert.Equal(ObservedOnUtc, rate.ObservedOnUtc);
        Assert.True(server.ReceivedPath(expectedPath));
    }

    internal static async Task HistoryMapsAsync(
        IExchangeGateway gateway,
        StubHttpServer server,
        string expectedPath)
    {
        var request = new HistoricalRatesRequest(
            gateway.Exchange,
            Symbol.Create("BTC-USDT"),
            FundingTime.AddDays(-1),
            FundingTime.AddDays(1),
            100);

        var result = await gateway.GetHistoricalRatesAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess, result.Error?.Code);
        var rate = Assert.Single(result.Value!);
        Assert.Equal(gateway.Exchange, rate.Exchange);
        Assert.Equal(Symbol.Create("BTC-USDT"), rate.Symbol);
        Assert.Equal(0.001m, rate.FundingRate);
        Assert.Equal(FundingTime, rate.FundingTime);
        Assert.Equal(ObservedOnUtc, rate.CollectedOnUtc);
        Assert.True(server.ReceivedPath(expectedPath));
    }

    internal static async Task HealthAvailableAsync(
        IExchangeGateway gateway,
        StubHttpServer server,
        string expectedPath)
    {
        var result = await gateway.CheckHealthAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ExchangeHealthStatus.Available, result.Status);
        Assert.Null(result.ErrorCode);
        Assert.True(server.ReceivedPath(expectedPath));
    }

    internal static async Task RateLimitedAsync(IExchangeGateway gateway)
    {
        var result = await gateway.GetCurrentRatesAsync(TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("Exchange.RateLimited", result.Error!.Code);
    }
}
