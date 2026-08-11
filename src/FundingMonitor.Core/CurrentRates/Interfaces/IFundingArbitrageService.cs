
namespace FundingMonitor.Core.CurrentRates.Interfaces;

public interface IFundingArbitrageService
{
    void UpdateOpportunities(IEnumerable<FundingArbitrageOpportunity> opportunities);

    IReadOnlyList<FundingArbitrageOpportunity> GetSortedByAprDiff(string? symbol = null,
        List<ExchangeType>? exchanges = null);
}