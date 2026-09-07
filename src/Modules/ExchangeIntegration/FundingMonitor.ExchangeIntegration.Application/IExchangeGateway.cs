using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.SharedKernel;

namespace FundingMonitor.ExchangeIntegration.Application;

public interface IExchangeGateway
{
    ExchangeId Exchange { get; }

    Task<Result<IReadOnlyList<CurrentRateSnapshot>>> GetCurrentRatesAsync(
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<HistoricalRatePoint>>> GetHistoricalRatesAsync(
        HistoricalRatesRequest request,
        CancellationToken cancellationToken);

    Task<ExchangeHealthSnapshot> CheckHealthAsync(CancellationToken cancellationToken);
}
