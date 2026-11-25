using FundingMonitor.Arbitrage.Calculation;

namespace FundingMonitor.Arbitrage.GetOpportunities;

public sealed class GetOpportunitiesHandler
{
    private readonly FundingArbitrageDetector _detector;

    internal GetOpportunitiesHandler(FundingArbitrageDetector detector)
    {
        _detector = detector;
    }

    internal async Task<IReadOnlyList<FundingArbitrageDto>> HandleAsync(
        GetOpportunitiesQuery query,
        CancellationToken cancellationToken)
    {
        var opportunities = await _detector.DetectAsync(cancellationToken);
        return ApplyFilters(opportunities, query).Select(ToDto).ToList().AsReadOnly();
    }

    private static IEnumerable<FundingArbitrageOpportunity> ApplyFilters(
        IReadOnlyList<FundingArbitrageOpportunity> opportunities,
        GetOpportunitiesQuery query)
    {
        IEnumerable<FundingArbitrageOpportunity> filtered = opportunities;

        if (!string.IsNullOrWhiteSpace(query.Symbol))
        {
            var requestedSymbol = query.Symbol.Trim();
            filtered = filtered.Where(opportunity =>
                opportunity.Symbol.Value.Equals(requestedSymbol, StringComparison.OrdinalIgnoreCase) ||
                BaseAsset(opportunity.Symbol.Value).Equals(requestedSymbol, StringComparison.OrdinalIgnoreCase));
        }

        if (query.Exchanges is { Count: > 0 })
        {
            if (query.Exchanges.Count < 2)
                return [];

            var selectedExchanges = query.Exchanges.ToHashSet();
            filtered = filtered.Where(opportunity =>
                selectedExchanges.Contains(opportunity.ExchangeA) &&
                selectedExchanges.Contains(opportunity.ExchangeB));
        }

        if (query.MinAprPercent is { } minAprPercent)
            filtered = filtered.Where(opportunity => opportunity.AprSpread >= minAprPercent);

        if (query.MaxAdversePriceSpreadPercent is { } maxAdverseSpreadPercent)
            filtered = filtered.Where(opportunity => opportunity.PriceSpreadPercent >= -maxAdverseSpreadPercent);

        if (query.SameFundingIntervalOnly)
            filtered = filtered.Where(opportunity =>
                opportunity.FundingIntervalHoursA is > 0 &&
                opportunity.FundingIntervalHoursA == opportunity.FundingIntervalHoursB);

        return filtered.OrderByDescending(opportunity => opportunity.AprSpread);
    }

    private static string BaseAsset(string symbol)
    {
        var separator = symbol.IndexOf('-');
        return separator < 0 ? symbol : symbol[..separator];
    }

    private static FundingArbitrageDto ToDto(FundingArbitrageOpportunity opportunity) => new(
        opportunity.Symbol.Value,
        opportunity.ExchangeA.Value,
        opportunity.ExchangeB.Value,
        opportunity.BidPriceA,
        opportunity.AskPriceA,
        opportunity.BidPriceB,
        opportunity.AskPriceB,
        opportunity.PriceSpread,
        opportunity.PriceSpreadPercent,
        opportunity.FundingRateA,
        opportunity.FundingRateB,
        opportunity.FundingRateSpread,
        opportunity.AprA,
        opportunity.AprB,
        opportunity.AprSpread,
        opportunity.FundingIntervalHoursA,
        opportunity.FundingIntervalHoursB,
        opportunity.PaymentsA,
        opportunity.PaymentsB,
        opportunity.ShortExchange?.Value,
        opportunity.LongExchange?.Value,
        opportunity.ExchangeA.ToTradeUrl(opportunity.Symbol.Value),
        opportunity.ExchangeB.ToTradeUrl(opportunity.Symbol.Value));
}
