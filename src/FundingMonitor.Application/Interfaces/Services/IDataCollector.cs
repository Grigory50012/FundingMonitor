using FundingMonitor.Core.Entities;

namespace FundingMonitor.Application.Interfaces.Services;

public interface IDataCollector
{
    Task<List<NormalizedFundingRate>> CollectAllRatesAsync(CancellationToken ct);
}