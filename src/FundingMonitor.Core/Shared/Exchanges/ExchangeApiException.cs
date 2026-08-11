using System.Net;

namespace FundingMonitor.Core.Shared.Exchanges;

public sealed class ExchangeApiException : Exception
{
    public ExchangeType Exchange { get; }
    public HttpStatusCode? HttpStatusCode { get; }
    public string? ExchangeErrorCode { get; }
    public FailureCategory FailureCategory { get; }

    public ExchangeApiException(
        ExchangeType exchange,
        string message,
        HttpStatusCode? httpStatusCode = null,
        string? exchangeErrorCode = null,
        FailureCategory failureCategory = FailureCategory.Unknown,
        Exception? innerException = null)
        : base($"{exchange}: {message}", innerException)
    {
        Exchange = exchange;
        HttpStatusCode = httpStatusCode;
        ExchangeErrorCode = exchangeErrorCode;
        FailureCategory = failureCategory;
    }

    public ExchangeApiException(
        ExchangeType exchange,
        string message,
        int exchangeErrorCode)
        : this(
            exchange,
            message,
            httpStatusCode: null,
            exchangeErrorCode: exchangeErrorCode.ToString())
    {
    }
}
