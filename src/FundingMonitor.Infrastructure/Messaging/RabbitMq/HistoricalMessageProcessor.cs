using FundingMonitor.Core.Configuration;
using FundingMonitor.Core.Failures;
using FundingMonitor.Core.Interfaces.Services;
using FundingMonitor.Core.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.Infrastructure.Messaging.RabbitMq;

public sealed class HistoricalMessageProcessor
{
    private readonly IHistoricalFundingRateCollector _collector;
    private readonly IHistoricalCollectionFailureClassifier _classifier;
    private readonly IRabbitMqMessagePublisher _publisher;
    private readonly IHistoricalRetryDelay _retryDelay;
    private readonly TimeProvider _timeProvider;
    private readonly HistoricalDataCollectionOptions _options;
    private readonly ILogger<HistoricalMessageProcessor> _logger;

    public HistoricalMessageProcessor(
        IHistoricalFundingRateCollector collector,
        IHistoricalCollectionFailureClassifier classifier,
        IRabbitMqMessagePublisher publisher,
        IHistoricalRetryDelay retryDelay,
        TimeProvider timeProvider,
        IOptions<HistoricalDataCollectionOptions> options,
        ILogger<HistoricalMessageProcessor> logger)
    {
        _collector = collector;
        _classifier = classifier;
        _publisher = publisher;
        _retryDelay = retryDelay;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<HistoricalMessageDisposition> ProcessAsync(
        HistoricalCollectionTask task,
        CancellationToken applicationStoppingToken)
    {
        try
        {
            await _collector.CollectAndSaveAsync(task, applicationStoppingToken);
            return HistoricalMessageDisposition.Ack;
        }
        catch (Exception exception)
        {
            var decision = _classifier.Classify(
                exception,
                applicationStoppingToken);

            if (decision == HistoricalCollectionFailureDecision.AbandonForShutdown)
                return HistoricalMessageDisposition.LeaveUnacked;

            if (decision == HistoricalCollectionFailureDecision.DeadLetter ||
                task.RetryCount >= _options.MaxRetries)
            {
                return await PublishDeadLetterAsync(
                    task,
                    exception,
                    decision,
                    applicationStoppingToken);
            }

            return await PublishRetryAsync(
                task,
                exception,
                applicationStoppingToken);
        }
    }

    private async Task<HistoricalMessageDisposition> PublishRetryAsync(
        HistoricalCollectionTask task,
        Exception exception,
        CancellationToken applicationStoppingToken)
    {
        try
        {
            await _retryDelay.WaitAsync(
                TimeSpan.FromSeconds(_options.RetryDelaySeconds),
                applicationStoppingToken);
        }
        catch (OperationCanceledException)
            when (applicationStoppingToken.IsCancellationRequested)
        {
            return HistoricalMessageDisposition.LeaveUnacked;
        }

        var retryTask = task with { RetryCount = task.RetryCount + 1 };
        RabbitMqPublishResult result;
        try
        {
            result = await _publisher.PublishAsync(
                retryTask,
                RabbitMqTopology.GetRoutingKey(task.Exchange),
                CreateFailureHeaders(task, exception, "Transient"),
                applicationStoppingToken);
        }
        catch (Exception publishException)
            when (!applicationStoppingToken.IsCancellationRequested)
        {
            _logger.LogError(
                publishException,
                "Retry copy for task {TaskId} could not be published",
                task.TaskId);
            return HistoricalMessageDisposition.LeaveUnacked;
        }
        catch (OperationCanceledException)
            when (applicationStoppingToken.IsCancellationRequested)
        {
            return HistoricalMessageDisposition.LeaveUnacked;
        }

        if (!result.Confirmed || result.Returned)
        {
            _logger.LogError(
                "Retry copy for task {TaskId} was not confirmed: {Error}",
                task.TaskId,
                result.Error);
            return HistoricalMessageDisposition.LeaveUnacked;
        }

        return HistoricalMessageDisposition.Ack;
    }

    private async Task<HistoricalMessageDisposition> PublishDeadLetterAsync(
        HistoricalCollectionTask task,
        Exception exception,
        HistoricalCollectionFailureDecision decision,
        CancellationToken applicationStoppingToken)
    {
        var category = decision == HistoricalCollectionFailureDecision.DeadLetter
            ? "Permanent"
            : "RetriesExhausted";
        RabbitMqPublishResult result;
        try
        {
            result = await _publisher.PublishAsync(
                task,
                RabbitMqTopology.DeadRoutingKey,
                CreateFailureHeaders(task, exception, category),
                applicationStoppingToken);
        }
        catch (Exception publishException)
            when (!applicationStoppingToken.IsCancellationRequested)
        {
            _logger.LogError(
                publishException,
                "Dead-letter copy for task {TaskId} could not be published",
                task.TaskId);
            return HistoricalMessageDisposition.LeaveUnacked;
        }
        catch (OperationCanceledException)
            when (applicationStoppingToken.IsCancellationRequested)
        {
            return HistoricalMessageDisposition.LeaveUnacked;
        }

        if (!result.Confirmed || result.Returned)
        {
            _logger.LogError(
                "Dead-letter copy for task {TaskId} was not confirmed: {Error}",
                task.TaskId,
                result.Error);
            return HistoricalMessageDisposition.LeaveUnacked;
        }

        return HistoricalMessageDisposition.Ack;
    }

    private Dictionary<string, object?> CreateFailureHeaders(
        HistoricalCollectionTask task,
        Exception exception,
        string category)
    {
        return new Dictionary<string, object?>
        {
            ["RetryCount"] = task.RetryCount,
            ["LastErrorType"] = exception.GetType().FullName ?? exception.GetType().Name,
            ["LastErrorMessage"] = exception.Message,
            ["FailedAt"] = _timeProvider.GetUtcNow().ToString("O"),
            ["FailureCategory"] = category
        };
    }
}
