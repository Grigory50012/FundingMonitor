using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Exceptions;

public class ExchangeApiException : Exception
{
    public ExchangeType Exchange { get; }
    public int? StatusCode { get; }
    
    public ExchangeApiException(ExchangeType exchange, string message, int? statusCode = null) 
        : base($"{exchange}: {message}")
    {
        Exchange = exchange;
        StatusCode = statusCode;
    }
}