using FundingMonitor.SharedKernel;

namespace FundingMonitor.CurrentRates.Contracts;

public sealed record FundingRateChangedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    ExchangeId Exchange,
    Symbol Symbol,
    DateTimeOffset? NextFundingTime) : IIntegrationEvent;
