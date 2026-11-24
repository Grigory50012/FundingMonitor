using FundingMonitor.Arbitrage.Contracts;
using FundingMonitor.Arbitrage.Domain;
using FundingMonitor.CurrentRates.Contracts;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Arbitrage.Application;

public sealed class FundingArbitrageDetector
{
    private readonly ICurrentRatesModule _currentRates;
    private readonly FundingArbitrageOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ArbitrageSnapshotCache _snapshots;

    public FundingArbitrageDetector(
        ICurrentRatesModule currentRates,
        IOptions<FundingArbitrageOptions> options)
        : this(currentRates, options, TimeProvider.System)
    {
    }

    public FundingArbitrageDetector(
        ICurrentRatesModule currentRates,
        IOptions<FundingArbitrageOptions> options,
        TimeProvider timeProvider,
        ArbitrageSnapshotCache? snapshots = null)
    {
        _currentRates = currentRates;
        _options = options.Value;
        _timeProvider = timeProvider;
        _snapshots = snapshots ?? new ArbitrageSnapshotCache();
    }

    public async Task<IReadOnlyList<FundingArbitrageOpportunity>> DetectAsync(
        CancellationToken cancellationToken)
    {
        var currentRates = await _currentRates.GetCurrentRatesAsync(
            new CurrentRatesQuery(null, null), cancellationToken);
        var now = _timeProvider.GetUtcNow();
        var cutoff = now.AddMinutes(-_options.MaxRateAgeMinutes);
        var rates = currentRates
            .Where(rate => rate.IsActive && rate.LastSeenOnUtc >= cutoff
                && rate.BidPrice is > 0m && rate.AskPrice is > 0m && rate.BidPrice <= rate.AskPrice
                && rate.FundingRate.HasValue && rate.FundingIntervalHours is > 0
                && rate.NextFundingTime > now)
            .ToArray();

        return _snapshots.GetOrCalculate(rates, Calculate);
    }

    private static IReadOnlyList<FundingArbitrageOpportunity> Calculate(IReadOnlyList<CurrentRateView> rates)
    {
        var opportunities = new List<FundingArbitrageOpportunity>();
        foreach (var symbolGroup in rates.GroupBy(rate => rate.Symbol))
        {
            var symbolRates = symbolGroup.ToArray();
            for (var first = 0; first < symbolRates.Length; first++)
            for (var second = first + 1; second < symbolRates.Length; second++)
            {
                var rateA = symbolRates[first];
                var rateB = symbolRates[second];
                if (rateA.Exchange == rateB.Exchange)
                    continue;

                opportunities.Add(new FundingArbitrageOpportunity
                {
                    Symbol = rateA.Symbol,
                    ExchangeA = rateA.Exchange,
                    ExchangeB = rateB.Exchange,
                    BidPriceA = rateA.BidPrice!.Value,
                    AskPriceA = rateA.AskPrice!.Value,
                    BidPriceB = rateB.BidPrice!.Value,
                    AskPriceB = rateB.AskPrice!.Value,
                    FundingRateA = rateA.FundingRate!.Value,
                    FundingRateB = rateB.FundingRate!.Value,
                    AprA = rateA.Apr,
                    AprB = rateB.Apr,
                    FundingIntervalHoursA = rateA.FundingIntervalHours,
                    FundingIntervalHoursB = rateB.FundingIntervalHours,
                    PaymentsA = PaymentsPerDay(rateA.FundingIntervalHours),
                    PaymentsB = PaymentsPerDay(rateB.FundingIntervalHours)
                });
            }
        }

        return opportunities.AsReadOnly();
    }

    private static int PaymentsPerDay(int? fundingIntervalHours) =>
        fundingIntervalHours is > 0 ? 24 / fundingIntervalHours.Value : 3;
}
