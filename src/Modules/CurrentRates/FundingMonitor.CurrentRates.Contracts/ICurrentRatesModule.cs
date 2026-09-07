namespace FundingMonitor.CurrentRates.Contracts;

public interface ICurrentRatesModule
{
    Task<IReadOnlyList<CurrentRateView>> GetCurrentRatesAsync(
        CurrentRatesQuery query,
        CancellationToken cancellationToken);
}
