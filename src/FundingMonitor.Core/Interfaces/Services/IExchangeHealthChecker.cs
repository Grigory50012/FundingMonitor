using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Services;

public interface IExchangeHealthChecker
{
    Task<Dictionary<ExchangeType, bool>> CheckAllExchangesAsync(CancellationToken cancellationToken);
}