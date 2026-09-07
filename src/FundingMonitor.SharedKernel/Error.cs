namespace FundingMonitor.SharedKernel;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Timeout,
    Unavailable,
    Unexpected
}

public sealed record Error(string Code, string Description, ErrorType Type);