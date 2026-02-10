using FundingMonitor.Core.Entities;

namespace FundingMonitor.Application.Interfaces.Services;

public interface IExchangeHealthChecker
{
    Task<Dictionary<ExchangeType, bool>> CheckAllExchangesAsync(CancellationToken cancellationToken);
}