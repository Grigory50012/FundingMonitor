namespace FundingMonitor.Core.Configuration;

public class CurrentDataCollectionOptions
{
    public const string SectionName = "CurrentDataCollectionOptions";

    public int UpdateIntervalSeconds { get; set; } = 5;
}