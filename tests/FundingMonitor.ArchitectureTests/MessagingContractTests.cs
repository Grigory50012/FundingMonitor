using FundingMonitor.Messaging;
using FundingMonitor.Messaging.Abstractions;
using Xunit;

namespace FundingMonitor.ArchitectureTests;

public sealed class MessagingContractTests
{
    [Fact]
    public void MessageEnvelope_PreservesTransportMetadata()
    {
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();
        var occurredOnUtc = DateTimeOffset.UtcNow;

        var envelope = new MessageEnvelope<string>(
            messageId,
            correlationId,
            causationId,
            occurredOnUtc,
            "test.message",
            1,
            "payload");

        Assert.Equal(messageId, envelope.MessageId);
        Assert.Equal(correlationId, envelope.CorrelationId);
        Assert.Equal(causationId, envelope.CausationId);
        Assert.Equal(occurredOnUtc, envelope.OccurredOnUtc);
        Assert.Equal("test.message", envelope.MessageType);
        Assert.Equal(1, envelope.Version);
        Assert.Equal("payload", envelope.Payload);
    }

    [Fact]
    public void MessagePublishResult_Success_IsConfirmedAndNotReturned()
    {
        var result = MessagePublishResult.Success;

        Assert.True(result.Confirmed);
        Assert.False(result.Returned);
        Assert.Null(result.Error);
    }
}