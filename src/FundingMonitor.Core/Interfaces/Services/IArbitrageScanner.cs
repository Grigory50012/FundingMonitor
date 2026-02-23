using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Services;

public interface IArbitrageScanner
{
    List<ArbitrageOpportunity> FindOpportunities(List<CurrentFundingRate> rates);
}