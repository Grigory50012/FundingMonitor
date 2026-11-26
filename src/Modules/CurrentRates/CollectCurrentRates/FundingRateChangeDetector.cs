using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.SharedKernel;
using Microsoft.Extensions.Logging;

namespace FundingMonitor.CurrentRates.CollectCurrentRates;

internal sealed class FundingRateChangeDetector(
    ILogger<FundingRateChangeDetector> logger)
{
    public IReadOnlyList<FundingRateChangedIntegrationEvent> DetectChanges(
        ExchangeId exchange,
        IReadOnlyDictionary<Symbol, CurrentFundingRate> previousRatesBySymbol,
        IReadOnlyCollection<CurrentFundingRate> observedRates,
        DateTimeOffset occurredOnUtc)
    {
        var fundingEvents = new List<FundingRateChangedIntegrationEvent>();

        foreach (var rate in observedRates)
        {
            var wasTracked = previousRatesBySymbol.TryGetValue(rate.Symbol, out var previousRate);
            if (wasTracked && previousRate!.NextFundingTime == rate.NextFundingTime)
                continue;

            var fundingEvent = new FundingRateChangedIntegrationEvent(
                Guid.NewGuid(),
                occurredOnUtc,
                exchange,
                rate.Symbol,
                rate.NextFundingTime);
            fundingEvents.Add(fundingEvent);

            logger.LogDebug(
                wasTracked
                    ? "Funding time changed: {Exchange}:{Symbol}; event {EventId}"
                    : "New symbol detected: {Exchange}:{Symbol}; event {EventId}",
                exchange,
                rate.Symbol, fundingEvent.EventId);
        }

        return fundingEvents;
    }
}
