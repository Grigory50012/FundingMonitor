using FundingMonitor.Core.Entities;

namespace FundingMonitor.Application.Interfaces.Repositories;

public interface ICurrentFundingRateRepository
{
    Task UpdateRatesAsync(IEnumerable<CurrentFundingRate> rates);
    Task<IEnumerable<CurrentFundingRate>> GetRatesAsync(string? symbol, List<ExchangeType>? exchanges);
}