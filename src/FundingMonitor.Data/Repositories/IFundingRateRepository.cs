using FundingMonitor.Core.Models;

namespace FundingMonitor.Data.Repositories;

public interface IFundingRateRepository
{
    Task SaveRatesAsync(IEnumerable<NormalizedFundingRate> rates);
}