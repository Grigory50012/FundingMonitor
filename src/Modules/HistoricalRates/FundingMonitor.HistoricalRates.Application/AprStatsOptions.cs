namespace FundingMonitor.HistoricalRates.Application;

public sealed class AprStatsOptions
{
    public const string SectionName = "AprStats";

    public int CacheDurationMinutes { get; set; } = 5;
    public int MaxHistoryDays { get; set; } = 30;
    public List<int> Periods { get; set; } = [1, 2, 3, 7, 14, 21, 30];
}
