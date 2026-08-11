using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Application.HistoricalRates.Collection;

/// <summary>
/// Собирает и идемпотентно сохраняет историю одной торговой пары.
/// RabbitMQ acknowledgements, retry и DLQ обрабатываются transport-слоем.
/// </summary>
public sealed class HistoricalFundingRateCollector : IHistoricalFundingRateCollector
{
    private readonly IEnumerable<IExchangeFundingRateClient> _clients;
    private readonly IHistoricalFundingRateRepository _repository;
    private readonly HistoricalDataCollectionOptions _options;
    private readonly ILogger<HistoricalFundingRateCollector> _logger;

    public HistoricalFundingRateCollector(
        IEnumerable<IExchangeFundingRateClient> clients,
        IHistoricalFundingRateRepository repository,
        IOptions<HistoricalDataCollectionOptions> options,
        ILogger<HistoricalFundingRateCollector> logger)
    {
        _clients = clients;
        _repository = repository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task CollectAndSaveAsync(
        HistoricalCollectionTask task,
        CancellationToken cancellationToken)
    {
        var client = _clients.FirstOrDefault(
            candidate => candidate.ExchangeType == task.Exchange);
        if (client is null)
        {
            throw new ExchangeApiException(
                task.Exchange,
                "Historical funding rate client is not registered",
                failureCategory: FailureCategory.Permanent);
        }

        var toTime = DateTime.UtcNow;
        var fromTime = toTime.AddMonths(-_options.MaxHistoryMonths);
        var rates = await client.GetHistoricalFundingRatesAsync(
            task.NormalizedSymbol,
            fromTime,
            toTime,
            _options.ApiPageSize,
            cancellationToken);

        if (rates.Count == 0)
        {
            _logger.LogWarning(
                "{Exchange}:{NormalizedSymbol} returned no historical rates",
                task.Exchange,
                task.NormalizedSymbol);
            return;
        }

        await _repository.AddRangeAsync(rates, cancellationToken);
        _logger.LogInformation(
            "{Exchange}:{NormalizedSymbol} collected {RatesCollected} historical rates",
            task.Exchange,
            task.NormalizedSymbol,
            rates.Count);
    }
}
