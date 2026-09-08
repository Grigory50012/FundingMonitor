namespace FundingMonitor.HistoricalRates.Infrastructure.Persistence;

internal sealed class HistoricalFundingRateDb
{
    public required string Exchange { get; set; }
    public required string NormalizedSymbol { get; set; }
    public decimal FundingRate { get; set; }
    public DateTimeOffset FundingTime { get; set; }
    public DateTimeOffset CollectedOnUtc { get; set; }
}
