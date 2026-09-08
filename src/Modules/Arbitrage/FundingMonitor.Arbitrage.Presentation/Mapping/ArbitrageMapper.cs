using FundingMonitor.Arbitrage.Contracts;
using FundingMonitor.Arbitrage.Presentation.Dtos;

namespace FundingMonitor.Arbitrage.Presentation.Mapping;

public static class ArbitrageMapper
{
    public static List<FundingArbitrageDto> ToDtoList(
        this IEnumerable<ArbitrageOpportunityView> opportunities) =>
        opportunities.Select(ToDto).ToList();

    private static FundingArbitrageDto ToDto(ArbitrageOpportunityView opportunity) => new(
        opportunity.Symbol.Value,
        opportunity.ExchangeA.Value,
        opportunity.ExchangeB.Value,
        opportunity.PriceA,
        opportunity.PriceB,
        opportunity.PriceSpread,
        opportunity.PriceSpreadPercent,
        opportunity.FundingRateA,
        opportunity.FundingRateB,
        opportunity.FundingRateSpread,
        opportunity.AprA,
        opportunity.AprB,
        opportunity.AprSpread,
        opportunity.PaymentsA,
        opportunity.PaymentsB,
        opportunity.ShortExchange.Value,
        opportunity.LongExchange.Value,
        opportunity.ExchangeA.ToTradeUrl(opportunity.Symbol.Value),
        opportunity.ExchangeB.ToTradeUrl(opportunity.Symbol.Value));
}
