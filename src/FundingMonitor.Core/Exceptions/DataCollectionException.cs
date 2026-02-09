namespace FundingMonitor.Core.Exceptions;

public class DataCollectionException : Exception
{
    public DataCollectionException(string message, Exception innerException = null) 
        : base(message, innerException) { }
}