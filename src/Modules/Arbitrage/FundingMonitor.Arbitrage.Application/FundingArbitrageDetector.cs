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

    public FundingArbitrageDetector(
        ICurrentRatesModule currentRates,
        IOptions<FundingArbitrageOptions> options)
        : this(currentRates, options, TimeProvider.System)
    {
    }

    public FundingArbitrageDetector(
        ICurrentRatesModule currentRates,
        IOptions<FundingArbitrageOptions> options,
        TimeProvider timeProvider)
    {
        _currentRates = currentRates;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<FundingArbitrageOpportunity>> DetectAsync(
        CancellationToken cancellationToken)
    {
        var currentRates = await _currentRates.GetCurrentRatesAsync(
            new CurrentRatesQuery(null, null), cancellationToken);
        var cutoff = _timeProvider.GetUtcNow().AddMinutes(-_options.MaxRateAgeMinutes);
        var rates = currentRates
            .Where(rate => rate.IsActive && rate.LastSeenOnUtc >= cutoff)
            .ToArray();

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

                if (Math.Abs(rateA.Apr - rateB.Apr) < _options.MinSpreadPercent)
                    continue;

                opportunities.Add(new FundingArbitrageOpportunity
                {
                    Symbol = rateA.Symbol,
                    ExchangeA = rateA.Exchange,
                    ExchangeB = rateB.Exchange,
                    PriceA = rateA.MarkPrice,
                    PriceB = rateB.MarkPrice,
                    FundingRateA = rateA.FundingRate,
                    FundingRateB = rateB.FundingRate,
                    AprA = rateA.Apr,
                    AprB = rateB.Apr,
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
