using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates.Domain;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.CurrentRates.Application.Collection;

public sealed class FundingRateChangeDetector(
    ILogger<FundingRateChangeDetector> logger) : IFundingRateChangeDetector
{
    public IReadOnlyList<FundingRateChangedIntegrationEvent> DetectChanges(
        ExchangeId exchange,
        IReadOnlyDictionary<Symbol, CurrentFundingRate> previous,
        IReadOnlyCollection<CurrentFundingRate> current,
        DateTimeOffset occurredOnUtc)
    {
        var events = new List<FundingRateChangedIntegrationEvent>();

        foreach (var rate in current)
        {
            if (previous.TryGetValue(rate.Symbol, out var existing) &&
                existing.NextFundingTime == rate.NextFundingTime)
                continue;

            events.Add(new FundingRateChangedIntegrationEvent(
                Guid.NewGuid(),
                occurredOnUtc,
                exchange,
                rate.Symbol,
                rate.NextFundingTime));

            logger.LogDebug(
                previous.ContainsKey(rate.Symbol)
                    ? "Funding time changed: {Exchange}:{Symbol}; event {EventId}"
                    : "New symbol detected: {Exchange}:{Symbol}; event {EventId}",
                exchange,
                rate.Symbol, events[^1].EventId);
        }

        return events;
    }
}
