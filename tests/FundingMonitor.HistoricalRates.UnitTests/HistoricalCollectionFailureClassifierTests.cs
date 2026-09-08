using System.Net;
using System.Text.Json;
using FundingMonitor.HistoricalRates.Application;
using FundingMonitor.SharedKernel;
using Xunit;

namespace FundingMonitor.HistoricalRates.UnitTests;

public sealed class HistoricalCollectionFailureClassifierTests
{
    [Fact]
    public void Classify_ShutdownCancellation_ReturnsAbandon()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        Assert.Equal(
            HistoricalCollectionFailureDecision.AbandonForShutdown,
            new HistoricalCollectionFailureClassifier().Classify(
                new OperationCanceledException(), source.Token));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Classify_Exception_ReturnsExpected(
        Exception exception,
        HistoricalCollectionFailureDecision expected)
    {
        Assert.Equal(expected, new HistoricalCollectionFailureClassifier().Classify(
            exception, CancellationToken.None));
    }

    public static TheoryData<Exception, HistoricalCollectionFailureDecision> Cases => new()
    {
        { new JsonException(), HistoricalCollectionFailureDecision.DeadLetter },
        { new TimeoutException(), HistoricalCollectionFailureDecision.Retry },
        { new HttpRequestException("bad", null, HttpStatusCode.BadRequest), HistoricalCollectionFailureDecision.DeadLetter },
        { new HttpRequestException("busy", null, HttpStatusCode.TooManyRequests), HistoricalCollectionFailureDecision.Retry },
        { new HistoricalExchangeRequestException(new Error("Exchange.InvalidRequest", "bad", ErrorType.Validation)), HistoricalCollectionFailureDecision.DeadLetter }
    };
}
