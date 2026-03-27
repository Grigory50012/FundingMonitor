using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Interfaces.Clients;
using FundingMonitor.Core.Interfaces.Queues;
using FundingMonitor.Core.Interfaces.Repositories;
using FundingMonitor.Core.Interfaces.Services;
using FundingMonitor.Core.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Application.Services;

/// <summary>
///     Сервис для сбора исторических данных о ставках финансирования
/// </summary>
public class HistoricalFundingRateCollector : IHistoricalFundingRateCollector
{
    private readonly IEnumerable<IExchangeFundingRateClient> _clients;
    private readonly ILogger<HistoricalFundingRateCollector> _logger;
    private readonly HistoricalDataCollectionOptions _options;
    private readonly IHistoryTaskQueue _queue;
    private readonly IHistoricalFundingRateRepository _repository;

    public HistoricalFundingRateCollector(
        IEnumerable<IExchangeFundingRateClient> clients,
        IHistoricalFundingRateRepository repository,
        IHistoryTaskQueue queue,
        IOptions<HistoricalDataCollectionOptions> options,
        ILogger<HistoricalFundingRateCollector> logger)
    {
        _clients = clients;
        _repository = repository;
        _queue = queue;
        _options = options.Value;
        _logger = logger;
    }

    public async Task CollectAndSaveAsync(
        HistoricalCollectionTask task,
        CancellationToken cancellationToken)
    {
        var retryCount = task.RetryCount;

        try
        {
            _logger.LogDebug(
                "[{Exchange}] Starting historical collection: {Symbol}",
                task.Exchange, task.NormalizedSymbol);

            // ✅ Получаем клиента для конкретной биржи
            var client = _clients.FirstOrDefault(c => c.ExchangeType == task.Exchange);

            if (client == null)
            {
                _logger.LogError("[{Exchange}] Client not found", task.Exchange);
                return;
            }

            // ✅ Получаем данные от биржи
            var fromTime = DateTime.UtcNow.AddMonths(-_options.MaxHistoryMonths);
            var rates = await client.GetHistoricalFundingRatesAsync(
                task.NormalizedSymbol,
                fromTime,
                DateTime.UtcNow,
                _options.ApiPageSize,
                cancellationToken);

            // ✅ Сохраняем в БД
            if (rates.Count > 0)
            {
                await _repository.AddRangeAsync(rates, cancellationToken);
                _logger.LogInformation(
                    "✅ {Exchange}:{Symbol} collected {Count} rates",
                    task.Exchange, task.NormalizedSymbol, rates.Count);
            }
            else
            {
                _logger.LogWarning("⚠️ {Exchange}:{Symbol} No data", task.Exchange, task.NormalizedSymbol);
            }
        }
        catch (Exception ex)
        {
            // ✅ Retry логика
            retryCount++;

            if (retryCount >= _options.MaxRetries)
            {
                _logger.LogError(
                    ex,
                    "❌ {Exchange}:{Symbol} Task failed after {RetryCount} attempts",
                    task.Exchange, task.NormalizedSymbol, retryCount);
                return;
            }

            // ✅ Возвращаем задачу в очередь для повторной попытки
            task.RetryCount = retryCount;
            await _queue.EnqueueAsync(task, cancellationToken);

            _logger.LogWarning(
                ex,
                "⚠️ {Exchange}:{Symbol} Task will be retried (attempt {RetryCount}/{MaxRetries})",
                task.Exchange, task.NormalizedSymbol, retryCount, _options.MaxRetries);
        }
    }
}