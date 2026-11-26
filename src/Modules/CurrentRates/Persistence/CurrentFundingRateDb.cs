namespace FundingMonitor.CurrentRates.Persistence;

internal sealed class CurrentFundingRateDb
{
    public int Id { get; init; }
    public required string Exchange { get; init; }
    public required string NormalizedSymbol { get; init; }
    public required string BaseAsset { get; set; }
    public required string QuoteAsset { get; set; }
    public decimal MarkPrice { get; set; }
    public decimal? BidPrice { get; set; }
    public decimal? AskPrice { get; set; }
    public decimal IndexPrice { get; set; }
    public decimal? FundingRate { get; set; }
    public int? FundingIntervalHours { get; set; }
    public DateTimeOffset? NextFundingTime { get; set; }
    public DateTimeOffset LastSeenOnUtc { get; set; }
    public bool IsActive { get; set; } = true;
}
