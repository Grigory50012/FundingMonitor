using System.Net;
using System.Text.Json;
using FundingMonitor.Core.Exceptions;
using FundingMonitor.Core.Failures;
using FundingMonitor.Core.Interfaces.Services;

namespace FundingMonitor.Application.Services;

public sealed class HistoricalCollectionFailureClassifier
    : IHistoricalCollectionFailureClassifier
{
    private static readonly HashSet<HttpStatusCode> TransientHttpStatuses =
    [
        HttpStatusCode.RequestTimeout,
        (HttpStatusCode)425,
        HttpStatusCode.TooManyRequests
    ];

    public HistoricalCollectionFailureDecision Classify(
        Exception exception,
        CancellationToken applicationStoppingToken)
    {
        if (exception is OperationCanceledException &&
            applicationStoppingToken.IsCancellationRequested)
        {
            return HistoricalCollectionFailureDecision.AbandonForShutdown;
        }

        if (exception is JsonException)
            return HistoricalCollectionFailureDecision.DeadLetter;

        if (exception is TimeoutException)
            return HistoricalCollectionFailureDecision.Retry;

        if (exception is HttpRequestException httpException)
            return ClassifyHttpStatus(httpException.StatusCode);

        if (exception is ExchangeApiException exchangeException)
        {
            if (exchangeException.FailureCategory == FailureCategory.Permanent)
                return HistoricalCollectionFailureDecision.DeadLetter;

            if (exchangeException.FailureCategory == FailureCategory.Transient)
                return HistoricalCollectionFailureDecision.Retry;

            return ClassifyHttpStatus(exchangeException.HttpStatusCode);
        }

        return HistoricalCollectionFailureDecision.Retry;
    }

    private static HistoricalCollectionFailureDecision ClassifyHttpStatus(
        HttpStatusCode? statusCode)
    {
        if (statusCode is null)
            return HistoricalCollectionFailureDecision.Retry;

        if (TransientHttpStatuses.Contains(statusCode.Value) ||
            (int)statusCode.Value >= 500)
        {
            return HistoricalCollectionFailureDecision.Retry;
        }

        return (int)statusCode.Value is >= 400 and < 500
            ? HistoricalCollectionFailureDecision.DeadLetter
            : HistoricalCollectionFailureDecision.Retry;
    }
}
