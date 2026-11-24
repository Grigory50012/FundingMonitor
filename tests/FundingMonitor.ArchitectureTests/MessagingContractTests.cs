using FundingMonitor.Messaging;
using FundingMonitor.Messaging.Abstractions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
            occurredOnUtc,
            "test.message",
            1,
            "payload");

        Assert.Equal(messageId, envelope.MessageId);
        Assert.Equal(correlationId, envelope.CorrelationId);
        Assert.Equal(occurredOnUtc, envelope.OccurredOnUtc);
        Assert.Equal("test.message", envelope.MessageType);
        Assert.Equal(1, envelope.Version);
        Assert.Equal("payload", envelope.Payload);

        var serialized = MessagingJson.SerializeToUtf8Bytes(envelope);
        using var document = JsonDocument.Parse(serialized);
        Assert.False(document.RootElement.TryGetProperty("causationId", out _));
        Assert.Equal(envelope, MessagingJson.Deserialize<MessageEnvelope<string>>(serialized));

        var legacyMessage = JsonNode.Parse(serialized)!.AsObject();
        legacyMessage["causationId"] = causationId;
        Assert.Equal(envelope, MessagingJson.Deserialize<MessageEnvelope<string>>(
            Encoding.UTF8.GetBytes(legacyMessage.ToJsonString())));
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
