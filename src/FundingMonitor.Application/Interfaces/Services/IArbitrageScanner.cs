using FundingMonitor.Core.Entities;

namespace FundingMonitor.Application.Interfaces.Services;

public interface IArbitrageScanner
{
    List<ArbitrageOpportunity> FindOpportunities(List<NormalizedFundingRate> rates);
}