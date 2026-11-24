using FundingMonitor.Arbitrage.Contracts;
using FundingMonitor.Arbitrage.Domain;

namespace FundingMonitor.Arbitrage.Application;

public sealed class ArbitrageModuleService : IArbitrageModule
{
    private readonly FundingArbitrageDetector _detector;
    private readonly FundingArbitrageService _service;

    public ArbitrageModuleService(
        FundingArbitrageDetector detector,
        FundingArbitrageService service)
    {
        _detector = detector;
        _service = service;
    }

    public async Task<IReadOnlyList<ArbitrageOpportunityView>> GetOpportunitiesAsync(
        ArbitrageQuery query,
        CancellationToken cancellationToken)
    {
        var opportunities = await _detector.DetectAsync(cancellationToken);
        return _service.Query(opportunities, query).Select(Map).ToList().AsReadOnly();
    }

    private static ArbitrageOpportunityView Map(FundingArbitrageOpportunity opportunity) => new(
        opportunity.Symbol,
        opportunity.ExchangeA,
        opportunity.ExchangeB,
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
        opportunity.ShortExchange,
        opportunity.LongExchange);
}
