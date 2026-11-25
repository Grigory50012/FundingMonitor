namespace FundingMonitor.HistoricalRates.Infrastructure.Persistence;

internal sealed class HistoricalRatesCleanupOptions
{
    public int RetentionDays { get; init; } = 90;
    public TimeOnly RunAtMoscowTime { get; init; } = new(4, 0);
}
