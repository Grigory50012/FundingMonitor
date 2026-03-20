namespace FundingMonitor.Core.Configuration;

public class CurrentDataCollectionOptions
{
    public const string SectionName = "CurrentDataCollection";

    public int UpdateIntervalSeconds { get; set; } = 5;

    public int CollectionTimeoutSeconds { get; set; } = 30;
}