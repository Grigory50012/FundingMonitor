using FundingMonitor.Core.Events;

namespace FundingMonitor.Core.Interfaces.Services;

public interface IFundingRateHistoryService
{
    Task ProcessDetectionEventsAsync(List<FundingEvent> events, CancellationToken cancellationToken);
}