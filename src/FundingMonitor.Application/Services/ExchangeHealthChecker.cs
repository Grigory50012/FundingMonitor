using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.Application.Services;

public class ExchangeHealthChecker : IExchangeHealthChecker
{
    private readonly IEnumerable<IExchangeApiClient> _clients;
    private readonly ILogger<ExchangeHealthChecker> _logger;

    public ExchangeHealthChecker(
        IEnumerable<IExchangeApiClient> clients,
        ILogger<ExchangeHealthChecker> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<Dictionary<ExchangeType, bool>> CheckAllExchangesAsync(CancellationToken cancellationToken)
    {
        var results = new Dictionary<ExchangeType, bool>();

        foreach (var client in _clients)
            try
            {
                var isAvailable = await client.IsAvailableAsync(cancellationToken);
                results[client.ExchangeType] = isAvailable;

                if (isAvailable)
                    _logger.LogInformation("{Exchange} доступна", client.ExchangeType);
                else
                    _logger.LogWarning("{Exchange} не доступна", client.ExchangeType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке {Exchange}", client.ExchangeType);
                results[client.ExchangeType] = false;
            }

        return results;
    }
}