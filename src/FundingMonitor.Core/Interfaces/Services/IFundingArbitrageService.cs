using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Services;

public interface IFundingArbitrageService
{
    void UpdateOpportunities(IEnumerable<FundingArbitrageOpportunity> opportunities);

    IReadOnlyList<FundingArbitrageOpportunity> GetSortedByAprDiff(string? symbol = null,
        List<ExchangeType>? exchanges = null);
}