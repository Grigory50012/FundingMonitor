using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Services;

public interface IArbitrageOpportunityFinder
{
    List<ArbitrageOpportunity> FindOpportunities(List<CurrentFundingRate> rates);
}