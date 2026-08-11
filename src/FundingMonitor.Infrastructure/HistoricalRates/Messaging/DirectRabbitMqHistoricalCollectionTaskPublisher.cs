using Microsoft.Extensions.Logging;

namespace FundingMonitor.Infrastructure.HistoricalRates.Messaging;

public sealed class DirectRabbitMqHistoricalCollectionTaskPublisher
    : IHistoricalCollectionTaskPublisher
{
    private readonly IRabbitMqMessagePublisher _messagePublisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DirectRabbitMqHistoricalCollectionTaskPublisher> _logger;

    public DirectRabbitMqHistoricalCollectionTaskPublisher(
        IRabbitMqMessagePublisher messagePublisher,
        TimeProvider timeProvider,
        ILogger<DirectRabbitMqHistoricalCollectionTaskPublisher> logger)
    {
        _messagePublisher = messagePublisher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task PublishAsync(
        IReadOnlyCollection<FundingRateChangedEvent> events,
        CancellationToken cancellationToken)
    {
        foreach (var @event in events)
        {
            var task = new HistoricalCollectionTask(
                Guid.NewGuid(),
                @event.Exchange,
                @event.NormalizedSymbol,
                _timeProvider.GetUtcNow().UtcDateTime);
            var routingKey = RabbitMqTopology.GetRoutingKey(@event.Exchange);
            await PublishWithRetryAsync(task, routingKey, cancellationToken);

            _logger.LogDebug(
                "Published historical task {TaskId} for {Exchange}:{NormalizedSymbol}",
                task.TaskId,
                task.Exchange,
                task.NormalizedSymbol);
        }
    }

    private async Task PublishWithRetryAsync(
        HistoricalCollectionTask task,
        string routingKey,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var result = await _messagePublisher.PublishAsync(
                    task,
                    routingKey,
                    headers: null,
                    cancellationToken);
                if (result.Confirmed && !result.Returned)
                    return;

                lastException = new RabbitMqPublishException(
                    $"RabbitMQ did not accept task {task.TaskId}: {result.Error}");
                if (result.Returned)
                    break;
            }
            catch (Exception exception)
                when (!cancellationToken.IsCancellationRequested)
            {
                lastException = exception;
            }

            if (attempt < 3)
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(100 * attempt),
                    _timeProvider,
                    cancellationToken);
            }
        }

        _logger.LogCritical(
            lastException,
            "Historical task {TaskId} for {Exchange}:{NormalizedSymbol} was not published",
            task.TaskId,
            task.Exchange,
            task.NormalizedSymbol);
        throw new RabbitMqPublishException(
            $"RabbitMQ did not confirm task {task.TaskId}",
            lastException ?? new InvalidOperationException("Unknown publication failure"));
    }
}
