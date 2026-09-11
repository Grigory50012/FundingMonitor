using System.Net;
using System.Text.Json;
using FundingMonitor.SharedKernel;

namespace FundingMonitor.HistoricalRates.Application;

public sealed class HistoricalCollectionFailureClassifier : IHistoricalCollectionFailureClassifier
{
    public HistoricalCollectionFailureDecision Classify(
        Exception exception,
        CancellationToken applicationStoppingToken)
    {
        if (exception is OperationCanceledException && applicationStoppingToken.IsCancellationRequested)
            return HistoricalCollectionFailureDecision.AbandonForShutdown;
        if (exception is JsonException)
            return HistoricalCollectionFailureDecision.DeadLetter;
        if (exception is TimeoutException)
            return HistoricalCollectionFailureDecision.Retry;
        if (exception is HttpRequestException http)
            return ClassifyStatus(http.StatusCode);
        if (exception is HistoricalExchangeRequestException exchange)
        {
            // Repeating credentials or geographic restrictions cannot repair this delivery.
            // Keep the public error type intact and send the first failure to confirmed DLQ.
            if (exchange.Error.Code is "Exchange.Authentication" or "Exchange.GeoRestricted")
                return HistoricalCollectionFailureDecision.DeadLetter;
            return exchange.Error.Type switch
            {
                ErrorType.Validation or ErrorType.NotFound or ErrorType.Conflict =>
                    HistoricalCollectionFailureDecision.DeadLetter,
                _ => HistoricalCollectionFailureDecision.Retry
            };
        }
        return HistoricalCollectionFailureDecision.Retry;
    }

    private static HistoricalCollectionFailureDecision ClassifyStatus(HttpStatusCode? status)
    {
        if (status is null || status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
            (int)status >= 500)
            return HistoricalCollectionFailureDecision.Retry;
        return (int)status is >= 400 and < 500
            ? HistoricalCollectionFailureDecision.DeadLetter
            : HistoricalCollectionFailureDecision.Retry;
    }
}
