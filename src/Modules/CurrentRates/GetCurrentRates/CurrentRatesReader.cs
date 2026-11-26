using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates.Persistence;

namespace FundingMonitor.CurrentRates.GetCurrentRates;

internal sealed class CurrentRatesReader(
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
            rate.LastSeenOnUtc,
            rate.IsActive,
            rate.Apr,
            rate.Deviation,
            rate.BidPrice,
            rate.AskPrice)).ToArray();
    }
}
