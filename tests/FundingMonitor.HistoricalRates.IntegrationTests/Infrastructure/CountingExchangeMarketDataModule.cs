using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.SharedKernel;

namespace FundingMonitor.HistoricalRates.IntegrationTests.Infrastructure;

internal sealed class CountingExchangeMarketDataModule(
    IReadOnlyList<HistoricalRatePoint> history) : IExchangeMarketDataModule
{
    public int HistoricalRequestCount { get; private set; }

    public Task<Result<IReadOnlyList<HistoricalRatePoint>>> GetHistoricalRatesAsync(
        HistoricalRatesRequest request,
        CancellationToken cancellationToken)
    {
        HistoricalRequestCount++;
        return Task.FromResult(Result<IReadOnlyList<HistoricalRatePoint>>.Success(history));
    }

    public Task<Result<IReadOnlyList<CurrentRateSnapshot>>> GetCurrentRatesAsync(
        ExchangeId exchange,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<ExchangeHealthSnapshot>> CheckHealthAsync(
        CancellationToken cancellationToken) => throw new NotSupportedException();
}
