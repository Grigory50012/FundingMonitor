using FundingMonitor.Application.Services;
using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Exceptions;
using FundingMonitor.Core.Failures;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Core.Queues;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FundingMonitor.UnitTests.Application;

public sealed class HistoricalFundingRateCollectorTests
{
    [Fact]
    public async Task CollectAndSaveAsync_ClientSucceeds_SavesRates()
    {
        var expected = new HistoricalFundingRate
        {
            Exchange = ExchangeType.Binance,
            NormalizedSymbol = "BTC-USDT",
            FundingRate = 0.0001m,
            FundingTime = new DateTime(2026, 7, 26, 16, 0, 0, DateTimeKind.Utc),
            CollectedAt = new DateTime(2026, 7, 26, 18, 0, 0, DateTimeKind.Utc)
        };
        var repository = new RecordingHistoricalRepository();
        var collector = CreateCollector(
            [new StubExchangeClient(ExchangeType.Binance, [expected])],
            repository);

        await collector.CollectAndSaveAsync(CreateTask(), CancellationToken.None);

        Assert.Equal(expected, Assert.Single(repository.Saved));
    }

    [Fact]
    public async Task CollectAndSaveAsync_ClientThrows_PropagatesException()
    {
        var expected = new TimeoutException("exchange timeout");
        var collector = CreateCollector(
            [new StubExchangeClient(ExchangeType.Binance, exception: expected)],
            new RecordingHistoricalRepository());

        var actual = await Assert.ThrowsAsync<TimeoutException>(() =>
            collector.CollectAndSaveAsync(CreateTask(), CancellationToken.None));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task CollectAndSaveAsync_ClientMissing_ThrowsPermanentFailure()
    {
        var collector = CreateCollector([], new RecordingHistoricalRepository());

        var exception = await Assert.ThrowsAsync<ExchangeApiException>(() =>
            collector.CollectAndSaveAsync(CreateTask(), CancellationToken.None));

        Assert.Equal(FailureCategory.Permanent, exception.FailureCategory);
    }

    private static HistoricalFundingRateCollector CreateCollector(
        IEnumerable<IExchangeFundingRateClient> clients,
        IHistoricalFundingRateRepository repository)
    {
        return new HistoricalFundingRateCollector(
            clients,
            repository,
            Options.Create(new HistoricalDataCollectionOptions
            {
                ApiPageSize = 1000,
                MaxHistoryMonths = 1
            }),
            NullLogger<HistoricalFundingRateCollector>.Instance);
    }

    private static HistoricalCollectionTask CreateTask() =>
        new(
            Guid.NewGuid(),
            ExchangeType.Binance,
            "BTC-USDT",
            DateTime.UtcNow);

    private sealed class StubExchangeClient : IExchangeFundingRateClient
    {
        private readonly List<HistoricalFundingRate> _rates;
        private readonly Exception? _exception;

        public StubExchangeClient(
            ExchangeType exchangeType,
            List<HistoricalFundingRate>? rates = null,
            Exception? exception = null)
        {
            ExchangeType = exchangeType;
            _rates = rates ?? [];
            _exception = exception;
        }

        public ExchangeType ExchangeType { get; }

        public Task<List<CurrentFundingRate>> GetCurrentFundingRatesAsync(
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<List<HistoricalFundingRate>> GetHistoricalFundingRatesAsync(
            string symbol,
            DateTime fromTime,
            DateTime toTime,
            int limit,
            CancellationToken cancellationToken)
        {
            return _exception is null
                ? Task.FromResult(_rates)
                : Task.FromException<List<HistoricalFundingRate>>(_exception);
        }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingHistoricalRepository
        : IHistoricalFundingRateRepository
    {
        public List<HistoricalFundingRate> Saved { get; } = [];

        public Task AddRangeAsync(
            IEnumerable<HistoricalFundingRate> rates,
            CancellationToken cancellationToken)
        {
            Saved.AddRange(rates);
            return Task.CompletedTask;
        }

        public Task<List<HistoricalFundingRate>> GetHistoryAsync(
            string symbol,
            List<ExchangeType>? exchanges,
            DateTime? from,
            DateTime? to,
            int? limit,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
