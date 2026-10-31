using Microsoft.Extensions.Logging;
using System.Net;
using FundingMonitor.ExchangeIntegration.Domain;
using FundingMonitor.ExchangeIntegration.Infrastructure.Gateways;
using Microsoft.Extensions.Options;
using OKX.Net;
using OKX.Net.Clients;
using OKX.Net.Objects.Options;
using Xunit;

namespace FundingMonitor.ExchangeIntegration.IntegrationTests.Gateways;

public sealed class OkxExchangeGatewayTests
{
    [Fact]
    public async Task GetCurrentRatesAsync_ValidPayload_MapsExchangeSymbolRateAndPrices()
    {
        await using var server = CreateServer();
        ConfigureCurrent(server);

        await GatewayAssertions.CurrentMapsAsync(CreateGateway(server), server, "/api/v5/public/instruments");
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ValidPayload_MapsFundingTimeAndCollectedTime()
    {
        await using var server = CreateServer();
        server.Respond("/api/v5/public/funding-rate-history", $$"""
            {"code":"0","msg":"","data":[{"instType":"SWAP","instId":"BTC-USDT-SWAP","fundingRate":"0.001","fundingTime":"{{GatewayAssertions.FundingTime.ToUnixTimeMilliseconds()}}","method":"actual","formulaType":"withRate","realizedRate":"0.001"}]}
            """);

        await GatewayAssertions.HistoryMapsAsync(CreateGateway(server), server, "/api/v5/public/funding-rate-history");
    }

    [Fact]
    public async Task CheckHealthAsync_Http200_ReturnsAvailable()
    {
        await using var server = CreateServer();
        server.Respond("/api/v5/public/time", $$"""
            {"code":"0","msg":"","data":[{"ts":"{{GatewayAssertions.ObservedOnUtc.ToUnixTimeMilliseconds()}}"}]}
            """);

        await GatewayAssertions.HealthAvailableAsync(CreateGateway(server), server, "/api/v5/public/time");
    }

    [Fact]
    public async Task GetCurrentRatesAsync_Http429_ReturnsExchangeRateLimited()
    {
        await using var server = CreateServer();
        server.Respond("/api/v5/public/instruments", "{\"code\":\"50011\",\"msg\":\"Too many requests\",\"data\":[]}",
            HttpStatusCode.TooManyRequests);

        await GatewayAssertions.RateLimitedAsync(CreateGateway(server));
    }

    [Fact]
    public async Task Partial_current_failure_emits_one_warning()
    {
        await using var server = CreateServer();
        ConfigureCurrent(server);
        server.Respond("/api/v5/public/funding-rate", "{}", HttpStatusCode.ServiceUnavailable);
        var logger = new TestLogger<OkxExchangeGateway>();
        await CreateGateway(server, logger).GetCurrentRatesAsync(TestContext.Current.CancellationToken);
        Assert.Single(logger.Levels, level => level == LogLevel.Warning);
    }
    private static StubHttpServer CreateServer()
    {
        var server = new StubHttpServer();
        server.Start();
        return server;
    }

    private static void ConfigureCurrent(StubHttpServer server)
    {
        server.Respond("/api/v5/public/instruments", """
            {"code":"0","msg":"","data":[{"instType":"SWAP","instId":"BTC-USDT-SWAP","uly":"BTC-USDT","instFamily":"BTC-USDT","category":"1","baseCcy":"","quoteCcy":"","settleCcy":"USDT","ctVal":"0.01","ctMult":"1","ctValCcy":"BTC","optType":"","stk":"","listTime":"0","expTime":"","lever":"100","tickSz":"0.1","lotSz":"1","minSz":"1","ctType":"linear","alias":"","state":"live","maxLmtSz":"","maxMktSz":"","maxTwapSz":"","maxIcebergSz":"","maxTriggerSz":"","maxStopSz":""}]}
            """);
        server.Respond("/api/v5/public/mark-price", $$"""
            {"code":"0","msg":"","data":[{"instType":"SWAP","instId":"BTC-USDT-SWAP","markPx":"100","ts":"{{GatewayAssertions.ObservedOnUtc.ToUnixTimeMilliseconds()}}"}]}
            """);
        server.Respond("/api/v5/market/index-tickers", $$"""
            {"code":"0","msg":"","data":[{"instId":"BTC-USDT","idxPx":"99","high24h":"101","sodUtc0":"99","open24h":"99","low24h":"98","sodUtc8":"99","ts":"{{GatewayAssertions.ObservedOnUtc.ToUnixTimeMilliseconds()}}"}]}
            """);
        server.Respond("/api/v5/public/funding-rate", $$"""
            {"code":"0","msg":"","data":[{"instType":"SWAP","instId":"BTC-USDT-SWAP","method":"current_period","formulaType":"withRate","fundingRate":"0.001","fundingTime":"{{GatewayAssertions.NextFundingTime.ToUnixTimeMilliseconds()}}","nextFundingTime":"{{GatewayAssertions.NextFundingTime.AddHours(8).ToUnixTimeMilliseconds()}}","minFundingRate":"-0.01","maxFundingRate":"0.01","settFundingRate":"0.001","settState":"settled","ts":"{{GatewayAssertions.ObservedOnUtc.ToUnixTimeMilliseconds()}}"}]}
            """);
    }

    private static OkxExchangeGateway CreateGateway(StubHttpServer server, ILogger<OkxExchangeGateway>? logger = null)
    {
        var address = server.BaseAddress.ToString().TrimEnd('/');
        var environment = OKXEnvironment.CreateCustom("test", address, address);
        var client = new OKXRestClient(
            null,
            null,
            Options.Create(new OKXRestOptions
            {
                Environment = environment,
                RateLimiterEnabled = false
            }));

        return new OkxExchangeGateway(
            client,
            new SymbolNormalizer(),
            new FixedTimeProvider(GatewayAssertions.ObservedOnUtc),
            logger ?? new TestLogger<OkxExchangeGateway>());
    }
}
