namespace FundingMonitor.HistoricalRates.Infrastructure.Messaging;

internal sealed record MalformedHistoricalMessage(
    string? OriginalMessageId,
    string OriginalBodyBase64,
    string ErrorType,
    string ErrorMessage);
