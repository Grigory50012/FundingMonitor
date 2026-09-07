using FundingMonitor.CurrentRates.Application.Collection;
using FundingMonitor.CurrentRates.Application.Persistence;
using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates.Domain;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FundingMonitor.CurrentRates.UnitTests;

public sealed class CurrentFundingRateCollectorTests
{
    [Fact]
    public async Task CollectAsync_OneExchangeFails_CommitsOtherSnapshotsAndContinues()
    {
        var persistence = new RecordingPersistence();
        var marketData = new StubMarketDataModule();
        var collector = new CurrentFundingRateCollector(
            marketData,
            persistence,
            persistence,
            new FundingRateChangeDetector(NullLogger<FundingRateChangeDetector>.Instance),
            TimeProvider.System,
            NullLogger<CurrentFundingRateCollector>.Instance);

        var result = await collector.CollectAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.SuccessfulExchanges);
        Assert.Equal(1, result.FailedExchanges);
        Assert.Equal(2, persistence.Stored.Count);
        Assert.DoesNotContain(persistence.Stored, item => item.Exchange == ExchangeId.Parse("Bybit"));
    }

    private sealed class StubMarketDataModule : IExchangeMarketDataModule
    {
        public Task<Result<IReadOnlyList<CurrentRateSnapshot>>> GetCurrentRatesAsync(
            ExchangeId exchange,
            CancellationToken cancellationToken)
        {
            if (exchange == ExchangeId.Parse("Bybit"))
                return Task.FromResult(Result<IReadOnlyList<CurrentRateSnapshot>>.Failure(
                    new Error("Exchange.Timeout", "Exchange request timed out.", ErrorType.Timeout)));

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
                    null,
                    DateTimeOffset.UtcNow)
            ];
            return Task.FromResult(Result<IReadOnlyList<CurrentRateSnapshot>>.Success(result));
        }

        public Task<Result<IReadOnlyList<HistoricalRatePoint>>> GetHistoricalRatesAsync(
            HistoricalRatesRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<ExchangeHealthSnapshot>> CheckHealthAsync(
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class RecordingPersistence : ICurrentRatesRepository, ICurrentRatesUnitOfWork
    {
        public List<(ExchangeId Exchange, IReadOnlyCollection<CurrentFundingRate> Rates)> Stored { get; } = [];

        public Task<IReadOnlyList<CurrentFundingRate>> GetAllRatesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CurrentFundingRate>>([]);

        public Task<IReadOnlyList<CurrentFundingRate>> GetRatesAsync(
            CurrentRatesQuery query,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task StoreSnapshotAsync(
            ExchangeId exchange,
            IReadOnlyCollection<CurrentFundingRate> rates,
            IReadOnlyCollection<FundingRateChangedIntegrationEvent> events,
            CancellationToken cancellationToken)
        {
            Stored.Add((exchange, rates));
            return Task.CompletedTask;
        }
    }
}
