using FundingMonitor.SharedKernel;

namespace FundingMonitor.ExchangeIntegration.Contracts;

public interface IExchangeMarketDataModule
{
    Task<Result<IReadOnlyList<CurrentRateSnapshot>>> GetCurrentRatesAsync(
        ExchangeId exchange,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<HistoricalRatePoint>>> GetHistoricalRatesAsync(
        HistoricalRatesRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExchangeHealthSnapshot>> CheckHealthAsync(
        CancellationToken cancellationToken);
}
