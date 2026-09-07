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

            logger.LogInformation(
                previous.ContainsKey(rate.Symbol)
                    ? "Funding time changed: {Exchange}:{Symbol}"
                    : "New symbol detected: {Exchange}:{Symbol}",
                exchange,
                rate.Symbol);
        }

        return events;
    }
}
