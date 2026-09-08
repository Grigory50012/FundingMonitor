using FundingMonitor.SharedKernel;

namespace FundingMonitor.HistoricalRates.Contracts;

public interface IHistoricalRatesModule
{
    Task<IReadOnlyList<HistoricalRateView>> GetHistoryAsync(
        HistoricalRatesQuery query,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AprPeriodStatsView>> GetAprStatsAsync(
        Symbol symbol,
        IReadOnlyCollection<ExchangeId>? exchanges,
        CancellationToken cancellationToken);
}
