namespace FundingMonitor.Core.Configuration;

public class DataCollectionOptions
{
    public const string SectionName = "DataCollection";
    
    public int IntervalMinutes { get; set; } = 1;
    public int CollectionTimeoutSeconds { get; set; } = 15;
    public int RetryCount { get; set; } = 3;
}