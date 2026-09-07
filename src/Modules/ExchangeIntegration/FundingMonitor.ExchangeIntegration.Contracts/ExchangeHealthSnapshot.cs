using FundingMonitor.SharedKernel;

namespace FundingMonitor.ExchangeIntegration.Contracts;

public enum ExchangeHealthStatus
{
    Available,
    Degraded,
    Unavailable
}

public sealed record ExchangeHealthSnapshot(
    ExchangeId Exchange,
    ExchangeHealthStatus Status,
    string? ErrorCode);
