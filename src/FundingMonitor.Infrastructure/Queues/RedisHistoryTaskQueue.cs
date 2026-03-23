using System.Text.Json;
using FundingMonitor.Core.Interfaces.Queues;
using FundingMonitor.Core.Queues;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FundingMonitor.Infrastructure.Queues;

/// <summary>
///     Персистентная очередь задач на сбор истории на основе Redis
/// </summary>
public class RedisHistoryTaskQueue : IHistoryTaskQueue
{
    private const string QueueKey = "funding_monitor:history_queue";
    private readonly IDatabase _db;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<RedisHistoryTaskQueue> _logger;

    public RedisHistoryTaskQueue(IConnectionMultiplexer redis, ILogger<RedisHistoryTaskQueue> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public int Count => (int)_db.ListLength(QueueKey);

    public async Task EnqueueAsync(HistoricalCollectionTask task, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(task, _jsonOptions);
        await _db.ListLeftPushAsync(QueueKey, json);

        _logger.LogDebug("Task pushed to Redis queue: {Exchange}:{Symbol} (queue size: {Count})",
            task.Exchange, task.NormalizedSymbol, Count);
    }

    public async Task<HistoricalCollectionTask?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var result = await _db.ListRightPopAsync(QueueKey);

        if (result.IsNullOrEmpty)
            return null;

        var task = JsonSerializer.Deserialize<HistoricalCollectionTask>(result.ToString(), _jsonOptions);

        _logger.LogDebug("Task popped from Redis queue: {Exchange}:{Symbol} (queue size: {Count})",
            task?.Exchange, task?.NormalizedSymbol, Count);

        return task;
    }

    public bool TryDequeue(out HistoricalCollectionTask? task)
    {
        var result = _db.ListRightPop(QueueKey);

        if (result.IsNullOrEmpty)
        {
            task = null;
            return false;
        }

        task = JsonSerializer.Deserialize<HistoricalCollectionTask>(result.ToString(), _jsonOptions);
        return true;
    }
}