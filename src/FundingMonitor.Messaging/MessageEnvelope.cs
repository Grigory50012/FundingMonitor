namespace FundingMonitor.Messaging;

public sealed record MessageEnvelope<T>(
    Guid MessageId,
    Guid CorrelationId,
    Guid? CausationId,
    DateTimeOffset OccurredOnUtc,
    string MessageType,
    int Version,
    T Payload);