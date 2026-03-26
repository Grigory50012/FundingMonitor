using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Application.Services;

public class ExchangeAvailabilityChecker : IExchangeAvailabilityChecker
{
    private readonly IEnumerable<IExchangeFundingRateClient> _clients;
    private readonly ILogger<ExchangeAvailabilityChecker> _logger;

    public ExchangeAvailabilityChecker(
        IEnumerable<IExchangeFundingRateClient> clients,
        ILogger<ExchangeAvailabilityChecker> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<Dictionary<ExchangeType, bool>> CheckAllExchangesAsync(CancellationToken cancellationToken)
    {
        var results = new Dictionary<ExchangeType, bool>();
        var tasks = _clients.Select(c => CheckClientAsync(c, cancellationToken));
        var clientResults = await Task.WhenAll(tasks);

        foreach (var result in clientResults) results[result.Exchange] = result.IsAvailable;

        return results;
    }

    private async Task<(ExchangeType Exchange, bool IsAvailable)> CheckClientAsync(
        IExchangeFundingRateClient client,
        CancellationToken cancellationToken)
    {
        try
        {
            var isAvailable = await client.IsAvailableAsync(cancellationToken);

            var logLevel = isAvailable ? LogLevel.Information : LogLevel.Warning;
            _logger.Log(logLevel, "{Exchange} is {Status}",
                client.ExchangeType, isAvailable ? "available" : "unavailable");

            return (client.ExchangeType, isAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking {Exchange}", client.ExchangeType);
            return (client.ExchangeType, false);
        }
    }
}