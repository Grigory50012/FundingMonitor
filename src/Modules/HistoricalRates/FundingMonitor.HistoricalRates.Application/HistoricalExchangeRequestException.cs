using FundingMonitor.SharedKernel;

namespace FundingMonitor.HistoricalRates.Application;

public sealed class HistoricalExchangeRequestException(Error error)
    : Exception($"{error.Code}: {error.Description}")
{
    public Error Error { get; } = error;
}
