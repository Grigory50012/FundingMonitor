using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.HistoricalRates.Application;
using FundingMonitor.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingMonitor.HistoricalRates.Infrastructure.Messaging;

internal sealed class HistoricalMessageProcessor(
    IHistoricalEventHandler handler,
    IHistoricalCollectionFailureClassifier classifier,
    IMessagePublisher publisher,
    IHistoricalRetryDelay retryDelay,
    IOptions<HistoricalDataCollectionOptions> options,
    HistoricalTransportMetrics metrics,
    ILogger<HistoricalMessageProcessor> logger)
{
    public async Task<HistoricalMessageDisposition> ProcessMalformedAsync(
        SharedKernel.ExchangeId exchange,
        ReadOnlyMemory<byte> originalBody,
        string? originalMessageId,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var messageId = Guid.TryParse(originalMessageId, out var parsedMessageId)
            ? parsedMessageId
            : Guid.NewGuid();
        var payload = new MalformedHistoricalMessage(
            originalMessageId,
            Convert.ToBase64String(originalBody.Span),
            exception.GetType().FullName ?? exception.GetType().Name,
            Truncate(exception.Message));
        var envelope = new MessageEnvelope<MalformedHistoricalMessage>(
            messageId,
            messageId,
            null,
            DateTimeOffset.UtcNow,
            "historical-rate.malformed",
            1,
            payload);
        var headers = new Dictionary<string, object?>
        {
            ["x-original-exchange"] = exchange.Value,
            ["x-last-error-type"] = payload.ErrorType,
            ["x-last-error-message"] = payload.ErrorMessage
        };

        try
        {
            var result = await publisher.PublishAsync(
                RabbitMqTopology.ExchangeName,
                RabbitMqTopology.DeadRoutingKey,
                envelope,
                headers,
                cancellationToken);
            if (!result.Confirmed || result.Returned)
                return HistoricalMessageDisposition.LeaveUnacked;

            metrics.DeadLetter(exchange);
            return HistoricalMessageDisposition.Ack;
        }
        catch (Exception publishException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(
                publishException,
                "Malformed historical message {MessageId} was not dead-lettered",
                originalMessageId);
            return HistoricalMessageDisposition.LeaveUnacked;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return HistoricalMessageDisposition.LeaveUnacked;
        }
    }

    public async Task<HistoricalMessageDisposition> ProcessAsync(
        FundingRateChangedIntegrationEvent integrationEvent,
        int retryCount,
        CancellationToken stoppingToken)
    {
        try
        {
            await handler.HandleAsync(integrationEvent, stoppingToken);
            return HistoricalMessageDisposition.Ack;
        }
        catch (Exception exception)
        {
            var decision = classifier.Classify(exception, stoppingToken);
            if (decision == HistoricalCollectionFailureDecision.AbandonForShutdown)
                return HistoricalMessageDisposition.LeaveUnacked;
            if (decision == HistoricalCollectionFailureDecision.DeadLetter ||
                retryCount >= options.Value.MaxRetries)
                return await PublishCopyAsync(
                    integrationEvent, retryCount, true, exception, stoppingToken);

            try
            {
                await retryDelay.WaitAsync(
                    TimeSpan.FromSeconds(options.Value.RetryDelaySeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return HistoricalMessageDisposition.LeaveUnacked;
            }
            return await PublishCopyAsync(
                integrationEvent, retryCount + 1, false, exception, stoppingToken);
        }
    }

    private async Task<HistoricalMessageDisposition> PublishCopyAsync(
        FundingRateChangedIntegrationEvent integrationEvent,
        int retryCount,
        bool deadLetter,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var envelope = new MessageEnvelope<FundingRateChangedIntegrationEvent>(
            integrationEvent.EventId,
            integrationEvent.EventId,
            null,
            integrationEvent.OccurredOnUtc,
            "funding-rate.changed",
            1,
            integrationEvent);
        var headers = new Dictionary<string, object?>
        {
            ["x-retry-count"] = retryCount,
            ["x-last-error-type"] = exception.GetType().FullName,
            ["x-last-error-message"] = Truncate(exception.Message)
        };
        MessagePublishResult result;
        try
        {
            result = await publisher.PublishAsync(
                RabbitMqTopology.ExchangeName,
                deadLetter
                    ? RabbitMqTopology.DeadRoutingKey
                    : RabbitMqTopology.GetRoutingKey(integrationEvent.Exchange),
                envelope,
                headers,
                cancellationToken);
        }
        catch (Exception publishException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(publishException, "Replacement for event {EventId} was not published", integrationEvent.EventId);
            return HistoricalMessageDisposition.LeaveUnacked;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return HistoricalMessageDisposition.LeaveUnacked;
        }

        if (!result.Confirmed || result.Returned)
            return HistoricalMessageDisposition.LeaveUnacked;
        if (deadLetter)
            metrics.DeadLetter(integrationEvent.Exchange);
        else
            metrics.Retry(integrationEvent.Exchange);
        return HistoricalMessageDisposition.Ack;
    }

    private static string Truncate(string value) => value[..Math.Min(value.Length, 1000)];
}
