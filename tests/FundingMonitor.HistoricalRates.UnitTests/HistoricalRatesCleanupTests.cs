using FundingMonitor.HistoricalRates.Infrastructure.Persistence;
using Xunit;

namespace FundingMonitor.HistoricalRates.UnitTests;

public sealed class HistoricalRatesCleanupTests
{
    [Theory]
    [InlineData("2026-11-25T00:59:00Z", 4, "2026-11-25T01:00:00Z")]
    [InlineData("2026-11-25T01:01:00Z", 4, "2026-11-26T01:00:00Z")]
    [InlineData("2026-11-25T22:30:00Z", 2, "2026-11-25T23:00:00Z")]
    public void NextRunUsesConfiguredMoscowTime(string now, int hour, string expected)
    {
        Assert.Equal(DateTimeOffset.Parse(expected),
            HistoricalRatesCleanupService.GetNextRun(DateTimeOffset.Parse(now), new TimeOnly(hour, 0)));
    }
}
