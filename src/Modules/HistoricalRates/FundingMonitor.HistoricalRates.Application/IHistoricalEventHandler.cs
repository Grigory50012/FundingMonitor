using FundingMonitor.CurrentRates.Contracts;

namespace FundingMonitor.HistoricalRates.Application;

public interface IHistoricalEventHandler
{
    Task HandleAsync(
        FundingRateChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken);
}
