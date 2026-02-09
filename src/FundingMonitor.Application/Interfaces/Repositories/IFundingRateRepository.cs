using FundingMonitor.Core.Entities;

namespace FundingMonitor.Application.Interfaces.Repositories;

public interface IFundingRateRepository
{
    Task SaveRatesAsync(IEnumerable<NormalizedFundingRate> rates);
    Task<IEnumerable<NormalizedFundingRate>> GetRatesAsync(string? symbol, List<ExchangeType>? exchanges);
}