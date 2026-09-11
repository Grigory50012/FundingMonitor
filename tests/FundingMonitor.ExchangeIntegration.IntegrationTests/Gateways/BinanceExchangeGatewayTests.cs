using System.Net;
using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Objects.Options;
using FundingMonitor.ExchangeIntegration.Domain;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.ExchangeIntegration.Infrastructure.Gateways;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FundingMonitor.ExchangeIntegration.IntegrationTests.Gateways;

public sealed class BinanceExchangeGatewayTests
{
    [Fact]
    public async Task GetCurrentRatesAsync_ValidPayload_MapsExchangeSymbolRateAndPrices()
    {
        await using var server = CreateServer();
        ConfigureCurrent(server);

        await GatewayAssertions.CurrentMapsAsync(CreateGateway(server), server, "/fapi/v1/premiumIndex");
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ValidPayload_MapsFundingTimeAndCollectedTime()
    {
        await using var server = CreateServer();
        server.Respond("/fapi/v1/fundingRate", $$"""
            [{"symbol":"BTCUSDT","fundingRate":"0.001","fundingTime":{{GatewayAssertions.FundingTime.ToUnixTimeMilliseconds()}},"markPrice":"100"}]
            """);

        await GatewayAssertions.HistoryMapsAsync(CreateGateway(server), server, "/fapi/v1/fundingRate");
    }

    [Fact]
    public async Task CheckHealthAsync_Http200_ReturnsAvailable()
    {
        await using var server = CreateServer();
        server.Respond("/fapi/v1/ping", "{}");

        await GatewayAssertions.HealthAvailableAsync(CreateGateway(server), server, "/fapi/v1/ping");
    }

    [Fact]
    public async Task GetCurrentRatesAsync_Http429_ReturnsExchangeRateLimited()
    {
        await using var server = CreateServer();
        server.Respond("/fapi/v1/premiumIndex", "{\"code\":-1003,\"msg\":\"Too many requests\"}",
            HttpStatusCode.TooManyRequests);
        server.Respond("/fapi/v1/fundingInfo", "[]");

        await GatewayAssertions.RateLimitedAsync(CreateGateway(server));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "{\"code\":-2015,\"msg\":\"Invalid API-key\"}", "Exchange.Authentication")]
    [InlineData(HttpStatusCode.Forbidden, "{\"code\":0,\"msg\":\"Service unavailable from a restricted location\"}", "Exchange.GeoRestricted")]
    public async Task GetHistoricalRatesAsync_StableHttpFailure_PreservesCodeAndUnavailableType(
        HttpStatusCode status, string payload, string expectedCode)
    {
        await using var server = CreateServer();
        server.Respond("/fapi/v1/fundingRate", payload, status);
        var gateway = CreateGateway(server);
        var request = new HistoricalRatesRequest(gateway.Exchange, Symbol.Create("BTC-USDT"),
            GatewayAssertions.FundingTime.AddDays(-1), GatewayAssertions.FundingTime.AddDays(1), 100);

        var result = await gateway.GetHistoricalRatesAsync(request, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Error!.Code);
        Assert.Equal(ErrorType.Unavailable, result.Error.Type);
        Assert.True(server.ReceivedPath("/fapi/v1/fundingRate"));
    }

    private static StubHttpServer CreateServer()
    {
        var server = new StubHttpServer();
        server.Start();
        return server;
    }

    private static void ConfigureCurrent(StubHttpServer server)
    {
        server.Respond("/fapi/v1/premiumIndex", $$"""
            [{"symbol":"BTCUSDT","markPrice":"100","indexPrice":"99","estimatedSettlePrice":"1","lastFundingRate":"0.001","interestRate":"0.0001","nextFundingTime":{{GatewayAssertions.NextFundingTime.ToUnixTimeMilliseconds()}},"time":{{GatewayAssertions.ObservedOnUtc.ToUnixTimeMilliseconds()}}}]
            """);
        server.Respond("/fapi/v1/fundingInfo", """
            [{"symbol":"BTCUSDT","adjustedFundingRateCap":"0.01","adjustedFundingRateFloor":"-0.01","fundingIntervalHours":8,"disclaimer":false}]
            """);
    }

    private static BinanceExchangeGateway CreateGateway(StubHttpServer server)
    {
        var address = server.BaseAddress.ToString().TrimEnd('/');
        var environment = BinanceEnvironment.CreateCustom(
            "test", address, address, address, address, address,
            address, address, address, address, address);
        var client = new BinanceRestClient(
            null,
            null,
            Options.Create(new BinanceRestOptions
            {
                Environment = environment,
                RateLimiterEnabled = false
            }));

        return new BinanceExchangeGateway(
            client,
            new SymbolNormalizer(),
            new FixedTimeProvider(GatewayAssertions.ObservedOnUtc),
            NullLogger<BinanceExchangeGateway>.Instance);
    }
}
