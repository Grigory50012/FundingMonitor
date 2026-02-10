using FundingMonitor.Application.Interfaces.Clients;
using FundingMonitor.Application.Interfaces.Services;
using FundingMonitor.Core.Entities;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Application.Services;

public class DataCollector : IDataCollector
{
    private readonly IEnumerable<IExchangeApiClient> _clients;
    private readonly ILogger<DataCollector> _logger;

    public DataCollector(IEnumerable<IExchangeApiClient> clients, ILogger<DataCollector> logger)
    {
        _clients = clients;
        _logger = logger;
    }
    
    public async Task<List<NormalizedFundingRate>> CollectAllRatesAsync(CancellationToken ct)
    {
        var tasks = _clients.Select(client => CollectFromExchangeAsync(client, ct));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }
    
    private async Task<List<NormalizedFundingRate>> CollectFromExchangeAsync(
        IExchangeApiClient client, CancellationToken ct)
    {
        try
        {
            return await client.GetAllFundingRatesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect from {Exchange}", client.ExchangeType);
            return new List<NormalizedFundingRate>();
        }
    }
}