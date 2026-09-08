using FundingMonitor.Arbitrage.Contracts;
using FundingMonitor.Arbitrage.Domain;
using FundingMonitor.CurrentRates.Contracts;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Arbitrage.Application;

public sealed class ArbitrageModuleService : IArbitrageModule
{
    private readonly FundingArbitrageDetector _detector;
    private readonly FundingArbitrageService _snapshot;

    public ArbitrageModuleService(
        FundingArbitrageDetector detector,
        FundingArbitrageService snapshot)
    {
        _detector = detector;
        _snapshot = snapshot;
    }

    public ArbitrageModuleService(
        ICurrentRatesModule currentRates,
        IOptions<FundingArbitrageOptions> options)
        : this(new FundingArbitrageDetector(currentRates, options), new FundingArbitrageService())
    {
    }

    public async Task<IReadOnlyList<ArbitrageOpportunityView>> GetOpportunitiesAsync(
        ArbitrageQuery query,
        CancellationToken cancellationToken)
    {
        var opportunities = await _detector.DetectAsync(cancellationToken);
        _snapshot.ReplaceSnapshot(opportunities);
        return _snapshot.Query(query).Select(Map).ToList().AsReadOnly();
    }

    private static ArbitrageOpportunityView Map(FundingArbitrageOpportunity opportunity) => new(
        opportunity.Symbol,
        opportunity.ExchangeA,
        opportunity.ExchangeB,
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
        opportunity.ShortExchange,
        opportunity.LongExchange);
}
