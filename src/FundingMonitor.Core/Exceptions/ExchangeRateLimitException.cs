using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Exceptions;

public class ExchangeRateLimitException : Exception
{
    public ExchangeRateLimitException(ExchangeType exchange, string message, Exception innerException)
        : base($"{exchange}: {message}", innerException)
    {
        Exchange = exchange;
    }

    public ExchangeType Exchange { get; }
}