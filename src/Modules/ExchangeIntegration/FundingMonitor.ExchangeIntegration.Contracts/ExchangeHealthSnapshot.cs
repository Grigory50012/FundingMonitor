using FundingMonitor.SharedKernel;

namespace FundingMonitor.ExchangeIntegration.Contracts;

public enum ExchangeHealthStatus
{
    Available = 0,
    Unavailable = 2
}

public sealed record ExchangeHealthSnapshot(
    ExchangeId Exchange,
    ExchangeHealthStatus Status,
    string? ErrorCode);
