using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FundingMonitor.UnitTests.Application;

public sealed class CurrentFundingRateCollectorTests
{
    [Fact]
    public async Task CollectFundingRatesAsync_SnapshotSaved_PublishesEveryDetectedEvent()
    {
        var calls = new List<string>();
        var repository = new RecordingCurrentRepository(calls);
        var publisher = new RecordingTaskPublisher(calls);
        var rates = new[]
        {
            CreateRate("BTC-USDT"),
            CreateRate("BTC-USDT")
        };
        var collector = new CurrentFundingRateCollector(
            [new StubExchangeClient(rates)],
            repository,
            publisher,
            NullLogger<CurrentFundingRateCollector>.Instance,
            new FundingRateChangeDetector(
                NullLogger<FundingRateChangeDetector>.Instance),
            Options.Create(new CurrentDataCollectionOptions
            {
                DeactivateMissingAfterMinutes = 5
            }));

        var result = await collector.CollectFundingRatesAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, publisher.Events.Count);
        Assert.Equal(
            ["update", "deactivate", "publish"],
            calls);
    }

    private static CurrentFundingRate CreateRate(string symbol) =>
        new()
        {
            Exchange = ExchangeType.Binance,
            NormalizedSymbol = symbol,
            BaseAsset = "BTC",
            QuoteAsset = "USDT",
            MarkPrice = 100m,
            IndexPrice = 100m,
            FundingRate = 0.0001m,
            FundingIntervalHours = 8,
            NextFundingTime = DateTime.UtcNow.AddHours(8),
            LastSeenAt = DateTime.UtcNow,
            IsActive = true
        };

    private sealed class StubExchangeClient(IEnumerable<CurrentFundingRate> rates)
        : IExchangeFundingRateClient
    {
        public ExchangeType ExchangeType => ExchangeType.Binance;

        public Task<List<CurrentFundingRate>> GetCurrentFundingRatesAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult(rates.ToList());

        public Task<List<HistoricalFundingRate>> GetHistoricalFundingRatesAsync(
            string symbol,
            DateTime fromTime,
            DateTime toTime,
            int limit,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingCurrentRepository(List<string> calls)
        : ICurrentFundingRateRepository
    {
        public Task UpdateExchangeAsync(
            ExchangeType exchange,
            IEnumerable<CurrentFundingRate> rates,
            CancellationToken cancellationToken)
        {
            calls.Add("update");
            return Task.CompletedTask;
        }

        public Task DeactivateStaleAsync(
            ExchangeType exchange,
            TimeSpan deactivateMissingAfter,
            CancellationToken cancellationToken)
        {
            calls.Add("deactivate");
            return Task.CompletedTask;
        }

        public Task<IEnumerable<CurrentFundingRate>> GetRatesAsync(
            string? symbol,
            List<ExchangeType>? exchanges,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IEnumerable<CurrentFundingRate>> GetAllRatesAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IEnumerable<CurrentFundingRate>>([]);
    }

    private sealed class RecordingTaskPublisher(List<string> calls)
        : IHistoricalCollectionTaskPublisher
    {
        public List<FundingRateChangedEvent> Events { get; } = [];

        public Task PublishAsync(
            IReadOnlyCollection<FundingRateChangedEvent> events,
            CancellationToken cancellationToken)
        {
            calls.Add("publish");
            Events.AddRange(events);
            return Task.CompletedTask;
        }
    }
}
