namespace FundingMonitor.CurrentRates.Application.Collection;

public sealed class CurrentDataCollectionOptions
{
    public const string SectionName = "CurrentDataCollectionOptions";

    public int UpdateIntervalSeconds { get; set; } = 10;

    public int DeactivateMissingAfterMinutes { get; set; } = 5;
}
