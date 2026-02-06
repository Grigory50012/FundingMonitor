using FundingMonitor.Core.Enums;
using FundingMonitor.Core.Models;

namespace FundingMonitor.Data.Repositories;

public interface IFundingRateRepository
{
    Task SaveRatesAsync(IEnumerable<NormalizedFundingRate> rates);
    Task <IEnumerable<NormalizedFundingRate>> GetRatesAsync(string? symbol, List<ExchangeType>? exchanges);
}