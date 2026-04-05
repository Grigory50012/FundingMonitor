using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Services;

public interface IFundingArbitrageService
{
    void UpdateOpportunities(IEnumerable<FundingArbitrageOpportunity> opportunities);
    IReadOnlyList<FundingArbitrageOpportunity> GetBySymbol(string symbol);
    IReadOnlyList<FundingArbitrageOpportunity> GetSortedByAprDiff();
}