using System.Collections.Concurrent;
using FundingMonitor.CurrentRates.CollectCurrentRates;
using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates;
using FundingMonitor.CurrentRates.Persistence;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FundingMonitor.CurrentRates.UnitTests;

public sealed class CurrentFundingRateCollectorTests
{
    [Fact]
    public async Task HostedCollection_SavesAndPollsBinanceAgainWhileOkxWaits()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));
        var repository = new RecordingPersistence();
        var marketData = new StubMarketDataModule(holdOkx: true, repository.BinanceStored.Task);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IExchangeMarketDataModule>(marketData);
        services.AddSingleton<ICurrentRatesRepository>(repository);
        services.AddScoped<CurrentFundingRateCollector>();
        await using var provider = services.BuildServiceProvider();
        using var background = new CurrentCollectionBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new CurrentDataCollectionOptions { UpdateIntervalSeconds = 1 }),
            NullLogger<CurrentCollectionBackgroundService>.Instance);

        await background.StartAsync(timeout.Token);
        try
        {
            await marketData.OkxWaiting.Task.WaitAsync(timeout.Token);
            await repository.BinanceStored.Task.WaitAsync(timeout.Token);
            Assert.True(await marketData.SecondBinancePoll.Task.WaitAsync(timeout.Token),
                "Binance must persist its first snapshot before fetching the second.");
            Assert.DoesNotContain(repository.Stored, item => item.Exchange == ExchangeId.Parse("OKX"));
        }
        finally
        {
            await background.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task CollectAsync_ExchangeRequestFails_DoesNotPersistSnapshot()
    {
        var persistence = new RecordingPersistence();
        var marketData = new StubMarketDataModule();
        var collector = new CurrentFundingRateCollector(
            marketData,
            persistence,
            NullLogger<CurrentFundingRateCollector>.Instance);

        await collector.CollectAsync(ExchangeId.Parse("Bybit"), TestContext.Current.CancellationToken);

        Assert.Empty(persistence.Stored);
    }

    private sealed class StubMarketDataModule(bool holdOkx = false, Task? binanceSnapshotStored = null) : IExchangeMarketDataModule
    {
        private int _binancePolls;
        public TaskCompletionSource OkxWaiting { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> SecondBinancePoll { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<Result<IReadOnlyList<CurrentRateSnapshot>>> GetCurrentRatesAsync(
            ExchangeId exchange,
            CancellationToken cancellationToken)
        {
            if (holdOkx && exchange == ExchangeId.Parse("OKX"))
            {
                OkxWaiting.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            if (exchange == ExchangeId.Parse("Binance") && Interlocked.Increment(ref _binancePolls) == 2)
                SecondBinancePoll.TrySetResult(binanceSnapshotStored?.IsCompletedSuccessfully == true);

            if (exchange == ExchangeId.Parse("Bybit"))
                return Result<IReadOnlyList<CurrentRateSnapshot>>.Failure(
                    new Error("Exchange.Timeout", "Exchange request timed out.", ErrorType.Timeout));

            IReadOnlyList<CurrentRateSnapshot> result =
            [
                new(
                    exchange,
                    Symbol.Create("BTC-USDT"),
                    100m,
                    100m,
                    0.0001m,
                    8,
                    DateTimeOffset.UtcNow.AddHours(8),
                    DateTimeOffset.UtcNow)
            ];
            return Result<IReadOnlyList<CurrentRateSnapshot>>.Success(result);
        }

        public Task<Result<IReadOnlyList<HistoricalRatePoint>>> GetHistoricalRatesAsync(
            HistoricalRatesRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<ExchangeHealthSnapshot>> CheckHealthAsync(
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class RecordingPersistence : ICurrentRatesRepository
    {
        public ConcurrentQueue<(ExchangeId Exchange, IReadOnlyCollection<CurrentFundingRate> Rates)> Stored { get; } = new();
        public TaskCompletionSource BinanceStored { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<IReadOnlyList<CurrentFundingRate>> GetRatesAsync(
            CurrentRatesQuery query,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<int> StoreSnapshotAsync(
            ExchangeId exchange,
            IReadOnlyCollection<CurrentFundingRate> rates,
            CancellationToken cancellationToken)
        {
            Stored.Enqueue((exchange, rates));
            if (exchange == ExchangeId.Parse("Binance"))
                BinanceStored.TrySetResult();
            return Task.FromResult(0);
        }
    }
}
