namespace FundingMonitor.Arbitrage.Contracts;

public interface IArbitrageModule
{
    Task<IReadOnlyList<ArbitrageOpportunityView>> GetOpportunitiesAsync(
        ArbitrageQuery query,
        CancellationToken cancellationToken);
}
