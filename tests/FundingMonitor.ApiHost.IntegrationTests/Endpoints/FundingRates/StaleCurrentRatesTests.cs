using System.Net.Http.Json;
using FundingMonitor.CurrentRates.Application.Collection;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.SharedKernel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace FundingMonitor.ApiHost.IntegrationTests.Endpoints.FundingRates;

[Collection(PostgreSqlCollection.Name)]
public sealed class StaleCurrentRatesTests(PostgreSqlFixture database) : IntegrationTestBase(database)
{
    [Fact]
    public async Task FailedCollection_AfterConfiguredCutoff_PublicQueryExpiresRateButPreservesLastValue()
    {
        var clock = new AdvancingClock();
        await Fixture.SeedAsync(new FundingRateBuilder().LastSeenAt(clock.Now.UtcDateTime).Build());
        await using var factory = Fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(clock);
            services.RemoveAll<IExchangeMarketDataModule>();
            services.AddSingleton<IExchangeMarketDataModule, UnavailableExchanges>();
            services.Configure<CurrentDataCollectionOptions>(options => options.DeactivateMissingAfterMinutes = 7);
        }));
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        Assert.Single((await client.GetFromJsonAsync<List<FundingRateDto>>("/api/v1/FundingRates", ct))!);
        clock.Now = clock.Now.AddMinutes(7);
        Assert.Single((await client.GetFromJsonAsync<List<FundingRateDto>>("/api/v1/FundingRates", ct))!);
        clock.Now = clock.Now.AddSeconds(1);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<ICurrentFundingRateCollector>().CollectAsync(ct);
            Assert.Equal(3, result.FailedExchanges);
        }

        Assert.Empty((await client.GetFromJsonAsync<List<FundingRateDto>>("/api/v1/FundingRates", ct))!);
        await using var verification = factory.Services.CreateAsyncScope();
        var stored = await verification.ServiceProvider.GetRequiredService<CurrentRatesDbContext>()
            .CurrentFundingRates.AsNoTracking().SingleAsync(ct);
        Assert.Equal(0.0001m, stored.FundingRate);
        Assert.Equal(clock.Now.AddMinutes(-7).AddSeconds(-1), stored.LastSeenOnUtc);
    }

    private sealed class AdvancingClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class UnavailableExchanges : IExchangeMarketDataModule
    {
        public Task<Result<IReadOnlyList<CurrentRateSnapshot>>> GetCurrentRatesAsync(ExchangeId exchange, CancellationToken cancellationToken) =>
            Task.FromResult(Result<IReadOnlyList<CurrentRateSnapshot>>.Failure(new Error("Exchange.Timeout", "Controlled outage", ErrorType.Timeout)));
        public Task<Result<IReadOnlyList<HistoricalRatePoint>>> GetHistoricalRatesAsync(HistoricalRatesRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<ExchangeHealthSnapshot>> CheckHealthAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
