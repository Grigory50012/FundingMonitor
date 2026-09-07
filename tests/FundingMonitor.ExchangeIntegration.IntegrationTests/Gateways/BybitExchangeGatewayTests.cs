using System.Net;
using Bybit.Net;
using Bybit.Net.Clients;
using Bybit.Net.Objects.Options;
using FundingMonitor.ExchangeIntegration.Domain;
using FundingMonitor.ExchangeIntegration.Infrastructure.Gateways;
using Microsoft.Extensions.Options;
using Xunit;

namespace FundingMonitor.ExchangeIntegration.IntegrationTests.Gateways;

public sealed class BybitExchangeGatewayTests
{
    [Fact]
    public async Task GetCurrentRatesAsync_ValidPayload_MapsExchangeSymbolRateAndPrices()
    {
        await using var server = CreateServer();
        server.Respond("/v5/market/tickers", $$"""
            {"retCode":0,"retMsg":"OK","result":{"category":"linear","list":[{"symbol":"BTCUSDT","lastPrice":"100","indexPrice":"99","markPrice":"100","prevPrice24h":"99","price24hPcnt":"0.01","highPrice24h":"101","lowPrice24h":"98","prevPrice1h":"99","openInterest":"1","openInterestValue":"100","turnover24h":"1000","volume24h":"10","fundingRate":"0.001","nextFundingTime":"{{GatewayAssertions.NextFundingTime.ToUnixTimeMilliseconds()}}","fundingIntervalHour":"8","ask1Size":"1","bid1Price":"99","ask1Price":"100","bid1Size":"1"}]},"retExtInfo":{},"time":{{GatewayAssertions.ObservedOnUtc.ToUnixTimeMilliseconds()}}}
            """);

        await GatewayAssertions.CurrentMapsAsync(CreateGateway(server), server, "/v5/market/tickers");
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ValidPayload_MapsFundingTimeAndCollectedTime()
    {
        await using var server = CreateServer();
        server.Respond("/v5/market/funding/history", $$"""
            {"retCode":0,"retMsg":"OK","result":{"category":"linear","list":[{"symbol":"BTCUSDT","fundingRate":"0.001","fundingRateTimestamp":"{{GatewayAssertions.FundingTime.ToUnixTimeMilliseconds()}}"}]},"retExtInfo":{},"time":{{GatewayAssertions.ObservedOnUtc.ToUnixTimeMilliseconds()}}}
            """);

        await GatewayAssertions.HistoryMapsAsync(CreateGateway(server), server, "/v5/market/funding/history");
    }

    [Fact]
    public async Task CheckHealthAsync_Http200_ReturnsAvailable()
    {
        await using var server = CreateServer();
        server.Respond("/v5/market/time", $$"""
            {"retCode":0,"retMsg":"OK","result":{"timeSecond":"{{GatewayAssertions.ObservedOnUtc.ToUnixTimeSeconds()}}","timeNano":"0"},"retExtInfo":{},"time":{{GatewayAssertions.ObservedOnUtc.ToUnixTimeMilliseconds()}}}
            """);

        await GatewayAssertions.HealthAvailableAsync(CreateGateway(server), server, "/v5/market/time");
    }

    [Fact]
    public async Task GetCurrentRatesAsync_Http429_ReturnsExchangeRateLimited()
    {
        await using var server = CreateServer();
        server.Respond("/v5/market/tickers", "{\"retCode\":10006,\"retMsg\":\"Too many requests\",\"result\":{},\"retExtInfo\":{},\"time\":0}",
            HttpStatusCode.TooManyRequests);

        await GatewayAssertions.RateLimitedAsync(CreateGateway(server));
    }

    private static StubHttpServer CreateServer()
    {
        var server = new StubHttpServer();
        server.Start();
        return server;
    }

    private static BybitExchangeGateway CreateGateway(StubHttpServer server)
    {
        var address = server.BaseAddress.ToString().TrimEnd('/');
        var environment = BybitEnvironment.CreateCustom("test", address, address);
        var client = new BybitRestClient(
            null,
            null,
            Options.Create(new BybitRestOptions
            {
                Environment = environment,
                RateLimiterEnabled = false
            }));

        return new BybitExchangeGateway(
            client,
            new SymbolNormalizer(),
            new FixedTimeProvider(GatewayAssertions.ObservedOnUtc),
            new TestLogger<BybitExchangeGateway>());
    }
}
