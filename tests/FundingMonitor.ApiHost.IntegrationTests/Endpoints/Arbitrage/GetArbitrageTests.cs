using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FundingMonitor.Arbitrage.Presentation.Dtos;
using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.SharedKernel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.Endpoints.Arbitrage;

public sealed class GetArbitrageTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory =
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<ICurrentRatesModule>();
                services.AddSingleton<ICurrentRatesModule>(new StubCurrentRates());
            });
        });

    [Fact]
    public async Task AppliesFiltersTogether()
    {
        using var client = _factory.CreateClient();
        var token = TestContext.Current.CancellationToken;
        const string endpoint = "/api/v1/Arbitrage";
        const string filters = "?symbol=BTC&exchanges=Binance,Bybit&minAprPercent=1&sameFundingIntervalOnly=true";
        var all = await client.GetFromJsonAsync<FundingArbitrageDto[]>(endpoint, token);
        Assert.NotNull(all);
        Assert.Equal(3, all.Length);
        var filtered = await client.GetFromJsonAsync<FundingArbitrageDto[]>(
            endpoint + filters + "&maxAdversePriceSpreadPercent=2", token);
        Assert.NotNull(filtered);
        var match = Assert.Single(filtered);
        Assert.Equal(-2m, match.PriceSpreadPercent);
        Assert.Equal(8, match.FundingIntervalHoursA);

        var empty = await client.GetFromJsonAsync<FundingArbitrageDto[]>(
            endpoint + filters + "&maxAdversePriceSpreadPercent=1.99", token);
        Assert.NotNull(empty);
        Assert.Empty(empty);
    }

    [Fact]
    public async Task OpenApiIncludesFilterParameters()
    {
        using var client = _factory.CreateClient();
        var schema = await client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(schema);
        var parameters = document.RootElement.GetProperty("paths").GetProperty("/api/v1/Arbitrage")
            .GetProperty("get").GetProperty("parameters").EnumerateArray()
            .Select(parameter => parameter.GetProperty("name").GetString());
        foreach (var name in new[] { "symbol", "exchanges", "minAprPercent", "maxAdversePriceSpreadPercent", "sameFundingIntervalOnly" })
            Assert.Contains(name, parameters);
    }

    [Theory]
    [InlineData("minAprPercent=-1")]
    [InlineData("maxAdversePriceSpreadPercent=abc")]
    [InlineData("sameFundingIntervalOnly=abc")]
    public async Task InvalidFilterReturnsBadRequest(string query)
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/Arbitrage?" + query,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();

    private sealed class StubCurrentRates : ICurrentRatesModule
    {
        public Task<IReadOnlyList<CurrentRateView>> GetCurrentRatesAsync(
            CurrentRatesQuery query, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CurrentRateView>>(
            [
                Rate("Binance", 99m, 0.00002m, 8),
                Rate("Bybit", 101m, 0.00001m, 8),
                Rate("OKX", 100m, 0.00001m, 4)
            ]);

        private static CurrentRateView Rate(string exchange, decimal price, decimal rate, int interval) =>
            new(ExchangeId.Parse(exchange), Symbol.Create("BTC-USDT"), price, price, rate, interval,
                null, null, DateTimeOffset.UtcNow, true, rate * 100m * 365m * 24m / interval, 0m);
    }
}
