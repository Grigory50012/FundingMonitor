using FundingMonitor.CurrentRates.Application.Persistence;
using FundingMonitor.CurrentRates.Contracts;

namespace FundingMonitor.CurrentRates.Application;

public sealed class CurrentRatesService(
    ICurrentRatesRepository repository) : ICurrentRatesModule
{
    public async Task<IReadOnlyList<CurrentRateView>> GetCurrentRatesAsync(
        CurrentRatesQuery query,
        CancellationToken cancellationToken)
    {
        var rates = await repository.GetRatesAsync(query, cancellationToken);
        return rates.Select(rate => new CurrentRateView(
            rate.Exchange,
            rate.Symbol,
            rate.MarkPrice,
            rate.IndexPrice,
            rate.FundingRate,
            rate.FundingIntervalHours,
            rate.NextFundingTime,
            rate.PredictedNextRate,
            rate.LastSeenOnUtc,
            rate.IsActive,
            rate.Apr,
            rate.Deviation)).ToArray();
    }
}
