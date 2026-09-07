using System.Text.Json;
using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.SharedKernel;

namespace FundingMonitor.CurrentRates.Infrastructure.Outbox;

internal static class OutboxEventSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(FundingRateChangedIntegrationEvent integrationEvent) =>
        JsonSerializer.Serialize(
            new FundingRateChangedPayload(
                integrationEvent.EventId,
                integrationEvent.OccurredOnUtc,
                integrationEvent.Exchange.Value,
                integrationEvent.Symbol.Value,
                integrationEvent.NextFundingTime),
            Options);

    public static FundingRateChangedIntegrationEvent Deserialize(string payload)
    {
        var value = JsonSerializer.Deserialize<FundingRateChangedPayload>(payload, Options)
                    ?? throw new JsonException("Outbox payload is empty.");
        return new FundingRateChangedIntegrationEvent(
            value.EventId,
            value.OccurredOnUtc,
            ExchangeId.Parse(value.Exchange),
            Symbol.Create(value.Symbol),
            value.NextFundingTime);
    }

    private sealed record FundingRateChangedPayload(
        Guid EventId,
        DateTimeOffset OccurredOnUtc,
        string Exchange,
        string Symbol,
        DateTimeOffset? NextFundingTime);
}
