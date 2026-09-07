using FundingMonitor.CurrentRates.Application.Persistence;
using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates.Domain;
using FundingMonitor.CurrentRates.IntegrationTests.Infrastructure;
using FundingMonitor.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FundingMonitor.CurrentRates.IntegrationTests.Outbox;

[Collection("CurrentRatesPostgreSql")]
public sealed class CurrentRatesOutboxTests(CurrentRatesPostgreSqlFixture fixture)
{
    [Fact]
    public async Task StoreSnapshotAsync_RateAndEvent_CommitsBoth()
    {
        await fixture.ResetAsync();
        await using var scope = await fixture.CreateScopeAsync();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<ICurrentRatesUnitOfWork>();

        await unitOfWork.StoreSnapshotAsync(
            ExchangeId.Parse("Binance"),
            [TestRate],
            [TestEvent],
            TestContext.Current.CancellationToken);

        await using var verification = await fixture.CreateDbContextAsync();
        Assert.Single(await verification.CurrentFundingRates.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
        var outbox = Assert.Single(await verification.OutboxMessages.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
        Assert.Null(outbox.ProcessedOnUtc);
    }

    [Fact]
    public async Task StoreSnapshotAsync_SaveFails_CommitsNeitherRateNorEvent()
    {
        await fixture.ResetAsync();
        await using var scope = await fixture.CreateScopeAsync(new ThrowingSaveChangesInterceptor());
        var unitOfWork = scope.ServiceProvider.GetRequiredService<ICurrentRatesUnitOfWork>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.StoreSnapshotAsync(
            ExchangeId.Parse("Binance"),
            [TestRate],
            [TestEvent],
            TestContext.Current.CancellationToken));

        await using var verification = await fixture.CreateDbContextAsync();
        Assert.Empty(await verification.CurrentFundingRates.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
        Assert.Empty(await verification.OutboxMessages.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StoreSnapshotAsync_MissingStaleRate_DeactivatesIt()
    {
        await fixture.ResetAsync();
        await using (var firstScope = await fixture.CreateScopeAsync())
        {
            var unitOfWork = firstScope.ServiceProvider.GetRequiredService<ICurrentRatesUnitOfWork>();
            await unitOfWork.StoreSnapshotAsync(
                ExchangeId.Parse("Binance"),
                [TestRate],
                [],
                TestContext.Current.CancellationToken);
        }

        await using (var secondScope = await fixture.CreateScopeAsync())
        {
            var unitOfWork = secondScope.ServiceProvider.GetRequiredService<ICurrentRatesUnitOfWork>();
            await unitOfWork.StoreSnapshotAsync(
                ExchangeId.Parse("Binance"),
                [],
                [],
                TestContext.Current.CancellationToken);
        }

        await using var verification = await fixture.CreateDbContextAsync();
        var rate = Assert.Single(await verification.CurrentFundingRates.AsNoTracking().ToListAsync(
            TestContext.Current.CancellationToken));
        Assert.False(rate.IsActive);
    }

    private static CurrentFundingRate TestRate => new()
    {
        Exchange = ExchangeId.Parse("Binance"),
        Symbol = Symbol.Create("BTC-USDT"),
        BaseAsset = "BTC",
        QuoteAsset = "USDT",
        MarkPrice = 100m,
        IndexPrice = 100m,
        FundingRate = 0.0001m,
        FundingIntervalHours = 8,
        NextFundingTime = new DateTimeOffset(2026, 9, 6, 16, 0, 0, TimeSpan.Zero),
        LastSeenOnUtc = new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero)
    };

    private static FundingRateChangedIntegrationEvent TestEvent => new(
        Guid.Parse("10000000-0000-0000-0000-000000000001"),
        new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero),
        ExchangeId.Parse("Binance"),
        Symbol.Create("BTC-USDT"),
        new DateTimeOffset(2026, 9, 6, 16, 0, 0, TimeSpan.Zero));

    private sealed class ThrowingSaveChangesInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromException<InterceptionResult<int>>(
                new InvalidOperationException("Simulated save failure."));
    }
}

[CollectionDefinition("CurrentRatesPostgreSql")]
public sealed class CurrentRatesPostgreSqlCollection :
    ICollectionFixture<CurrentRatesPostgreSqlFixture>;
