namespace FundingMonitor.Core.Configuration;

public class CurrentDataCollectionOptions
{
    public const string SectionName = "CurrentDataCollection";

    public int UpdateIntervalMinutes { get; set; } = 1;
}