using FundingMonitor.Application.Interfaces.Clients;
using FundingMonitor.Application.Interfaces.Services;
using FundingMonitor.Core.Entities;

namespace FundingMonitor.Application.Services;

public class ExchangeHealthChecker : IExchangeHealthChecker
{
    private readonly IEnumerable<IExchangeApiClient> _clients;

    public ExchangeHealthChecker(IEnumerable<IExchangeApiClient> clients)
    {
        _clients = clients;
    }

    public async Task<Dictionary<ExchangeType, bool>> CheckAllExchangesAsync(CancellationToken cancellationToken)
    {
        var tasks = _clients.ToDictionary(
            client => client.ExchangeType,
            client => client.IsAvailableAsync(cancellationToken));
        
        await Task.WhenAll(tasks.Values);
        
        return tasks.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Result);
    }
}