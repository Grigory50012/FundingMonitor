using FundingMonitor.Core.Entities;

namespace FundingMonitor.Application.Interfaces.Repositories;

public interface IFundingRateCurrentRepository
{
    Task UpdateRatesAsync(IEnumerable<CurrentFundingRate> rates);
    Task<IEnumerable<CurrentFundingRate>> GetRatesAsync(string? symbol, List<ExchangeType>? exchanges);
}