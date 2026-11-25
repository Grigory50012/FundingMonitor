using FundingMonitor.CurrentRates.Contracts;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Arbitrage.Calculation;

internal sealed class FundingArbitrageDetector(
    ICurrentRatesModule rates,
    IOptions<FundingArbitrageOptions> options,
    TimeProvider timeProvider)
{
    private readonly FundingArbitrageOptions _options = options.Value;

    public async Task<IReadOnlyList<FundingArbitrageOpportunity>> DetectAsync(
        CancellationToken cancellationToken)
    {
        var currentRates = await rates.GetCurrentRatesAsync(
            new CurrentRatesQuery(null, null), cancellationToken);
        var now = timeProvider.GetUtcNow();
        var freshnessCutoff = now.AddMinutes(-_options.MaxRateAgeMinutes);
        var eligibleRates = currentRates
            .Where(rate => rate.IsActive
                           && rate.LastSeenOnUtc >= freshnessCutoff
                           && rate is { BidPrice: > 0m, AskPrice: > 0m } && rate.BidPrice <= rate.AskPrice
                           && rate is { FundingRate: not null, FundingIntervalHours: > 0 }
                           && rate.NextFundingTime > now)
            .OrderBy(rate => rate.Symbol.Value, StringComparer.Ordinal)
            .ThenBy(rate => rate.Exchange.Value, StringComparer.Ordinal);

        return CalculatePairs(eligibleRates);
    }

    private static IReadOnlyList<FundingArbitrageOpportunity> CalculatePairs(IEnumerable<CurrentRateView> rates)
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

                var intervalHoursA = rateA.FundingIntervalHours!.Value;
                var intervalHoursB = rateB.FundingIntervalHours!.Value;
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
                    FundingIntervalHoursA = intervalHoursA,
                    FundingIntervalHoursB = intervalHoursB,
                    PaymentsA = 24 / intervalHoursA,
                    PaymentsB = 24 / intervalHoursB
                });
            }
        }

        return opportunities.AsReadOnly();
    }
}
