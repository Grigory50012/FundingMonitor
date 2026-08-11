using System.Net;
using System.Text.Json;
using Xunit;

namespace FundingMonitor.UnitTests.Application;

public sealed class HistoricalCollectionFailureClassifierTests
{
    private readonly HistoricalCollectionFailureClassifier _sut = new();

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData((HttpStatusCode)425)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public void Classify_TransientHttpStatus_ReturnsRetry(HttpStatusCode statusCode)
    {
        var exception = new ExchangeApiException(
            ExchangeType.Binance,
            "temporary",
            statusCode,
            exchangeErrorCode: null,
            FailureCategory.Transient);

        Assert.Equal(
            HistoricalCollectionFailureDecision.Retry,
            _sut.Classify(exception, CancellationToken.None));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public void Classify_PermanentHttpStatus_ReturnsDeadLetter(HttpStatusCode statusCode)
    {
        var exception = new ExchangeApiException(
            ExchangeType.Bybit,
            "permanent",
            statusCode,
            exchangeErrorCode: null,
            FailureCategory.Permanent);

        Assert.Equal(
            HistoricalCollectionFailureDecision.DeadLetter,
            _sut.Classify(exception, CancellationToken.None));
    }

    [Fact]
    public void Classify_JsonException_ReturnsDeadLetter()
    {
        Assert.Equal(
            HistoricalCollectionFailureDecision.DeadLetter,
            _sut.Classify(new JsonException("contract"), CancellationToken.None));
    }

    [Fact]
    public void Classify_ApplicationCancellation_ReturnsAbandonForShutdown()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.Equal(
            HistoricalCollectionFailureDecision.AbandonForShutdown,
            _sut.Classify(new OperationCanceledException(source.Token), source.Token));
    }

    [Fact]
    public void Classify_UnknownException_ReturnsRetry()
    {
        Assert.Equal(
            HistoricalCollectionFailureDecision.Retry,
            _sut.Classify(new InvalidOperationException("unknown"), CancellationToken.None));
    }
}
