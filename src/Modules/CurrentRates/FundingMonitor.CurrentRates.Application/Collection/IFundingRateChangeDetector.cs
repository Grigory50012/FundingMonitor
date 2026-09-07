using FundingMonitor.CurrentRates.Contracts;
using FundingMonitor.CurrentRates.Domain;
using FundingMonitor.SharedKernel;

namespace FundingMonitor.CurrentRates.Application.Collection;

public interface IFundingRateChangeDetector
{
    IReadOnlyList<FundingRateChangedIntegrationEvent> DetectChanges(
        ExchangeId exchange,
        IReadOnlyDictionary<Symbol, CurrentFundingRate> previous,
        IReadOnlyCollection<CurrentFundingRate> current,
        DateTimeOffset occurredOnUtc);
}
