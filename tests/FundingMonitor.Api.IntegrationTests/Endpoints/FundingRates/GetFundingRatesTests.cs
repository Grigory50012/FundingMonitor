using System.Net;
using System.Net.Http.Json;
using FundingMonitor.Api.IntegrationTests.Infrastructure;
using FundingMonitor.Api.IntegrationTests.Infrastructure.Collections;
using FundingMonitor.Api.IntegrationTests.TestData;
using FundingMonitor.Api.Models.Dtos;
using Xunit;

namespace FundingMonitor.Api.IntegrationTests.Endpoints.FundingRates;

[Collection(PostgreSqlCollection.Name)]
public sealed class GetFundingRatesTests(PostgreSqlFixture database)
    : IntegrationTestBase(database)
{
    [Fact]
    public async Task GetFundingRates_ActiveRatesExist_ReturnsMappedRatesInContractOrder()
    {
        await Fixture.SeedAsync(
            new FundingRateBuilder()
                .WithExchange("Bybit")
                .WithSymbol("ETH-USDT", "ETH")
                .WithFunding(3_500m, 0.0002m)
                .Build(),
            new FundingRateBuilder().Build());

        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = Fixture.Factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/v1/FundingRates",
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var result = await response.Content.ReadFromJsonAsync<List<FundingRateDto>>(
            cancellationToken);

        Assert.NotNull(result);
        Assert.Collection(
            result,
            rate =>
            {
                Assert.Equal("Binance", rate.Exchange);
                Assert.Equal("BTC-USDT", rate.Symbol);
                Assert.Equal(65_000.50m, rate.MarkPrice);
                Assert.Equal(0.0001m, rate.FundingRate);
                Assert.Equal(10.95m, rate.APR);
                Assert.Equal(3, rate.NumberOfPaymentsPerDay);
                Assert.Equal(
                    new DateTime(2026, 7, 16, 16, 0, 0, DateTimeKind.Utc),
                    rate.NextFundingTime);
            },
            rate =>
            {
                Assert.Equal("Bybit", rate.Exchange);
                Assert.Equal("ETH-USDT", rate.Symbol);
            });
    }

    [Fact]
    public async Task GetFundingRates_SymbolProvided_ReturnsOnlyMatchingBaseAsset()
    {
        await Fixture.SeedAsync(
            new FundingRateBuilder().Build(),
            new FundingRateBuilder()
                .WithExchange("Bybit")
                .WithSymbol("ETH-USDT", "ETH")
                .Build());

        var result = await GetRatesAsync("/api/v1/FundingRates?symbol=BTC");

        var rate = Assert.Single(result);
        Assert.Equal("BTC-USDT", rate.Symbol);
        Assert.Equal("Binance", rate.Exchange);
    }

    [Fact]
    public async Task GetFundingRates_OneExchangeProvided_ReturnsOnlyMatchingExchange()
    {
        await Fixture.SeedAsync(
            new FundingRateBuilder().Build(),
            new FundingRateBuilder()
                .WithExchange("Bybit")
                .WithSymbol("ETH-USDT", "ETH")
                .Build());

        var result = await GetRatesAsync("/api/v1/FundingRates?exchanges=Bybit");

        var rate = Assert.Single(result);
        Assert.Equal("Bybit", rate.Exchange);
        Assert.Equal("ETH-USDT", rate.Symbol);
    }

    [Fact]
    public async Task GetFundingRates_MultipleExchangesProvided_ReturnsEveryMatchingExchange()
    {
        await Fixture.SeedAsync(
            new FundingRateBuilder().Build(),
            new FundingRateBuilder()
                .WithExchange("Bybit")
                .WithSymbol("ETH-USDT", "ETH")
                .Build(),
            new FundingRateBuilder()
                .WithExchange("OKX")
                .WithSymbol("SOL-USDT", "SOL")
                .Build());

        var result = await GetRatesAsync(
            "/api/v1/FundingRates?exchanges=Binance,OKX");

        Assert.Equal(
            new[] { "Binance", "OKX" },
            result.Select(rate => rate.Exchange));
    }

    [Fact]
    public async Task GetFundingRates_SymbolAndExchangeProvided_AppliesBothFilters()
    {
        await Fixture.SeedAsync(
            new FundingRateBuilder().Build(),
            new FundingRateBuilder()
                .WithExchange("Bybit")
                .Build(),
            new FundingRateBuilder()
                .WithExchange("Bybit")
                .WithSymbol("ETH-USDT", "ETH")
                .Build());

        var result = await GetRatesAsync(
            "/api/v1/FundingRates?symbol=BTC&exchanges=Bybit");

        var rate = Assert.Single(result);
        Assert.Equal("Bybit", rate.Exchange);
        Assert.Equal("BTC-USDT", rate.Symbol);
    }

    [Fact]
    public async Task GetFundingRates_InactiveRateExists_DoesNotReturnRate()
    {
        await Fixture.SeedAsync(
            new FundingRateBuilder().Build(),
            new FundingRateBuilder()
                .WithExchange("Bybit")
                .Inactive()
                .Build());

        var result = await GetRatesAsync("/api/v1/FundingRates");

        var rate = Assert.Single(result);
        Assert.Equal("Binance", rate.Exchange);
    }

    [Fact]
    public async Task GetFundingRates_NoRateMatches_ReturnsEmptyJsonArray()
    {
        await Fixture.SeedAsync(new FundingRateBuilder().Build());

        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = Fixture.Factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/v1/FundingRates?symbol=ETH",
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private async Task<List<FundingRateDto>> GetRatesAsync(string requestUri)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = Fixture.Factory.CreateClient();

        using var response = await client.GetAsync(requestUri, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<List<FundingRateDto>>(
            await response.Content.ReadFromJsonAsync<List<FundingRateDto>>(
                cancellationToken));
    }
}
