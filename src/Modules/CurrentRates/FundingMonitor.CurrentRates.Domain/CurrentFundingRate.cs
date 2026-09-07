using FundingMonitor.SharedKernel;

namespace FundingMonitor.CurrentRates.Domain;

public sealed record CurrentFundingRate
{
    public required ExchangeId Exchange { get; init; }
    public required Symbol Symbol { get; init; }
    public required string BaseAsset { get; init; }
    public required string QuoteAsset { get; init; }
    public required decimal MarkPrice { get; init; }
    public required decimal IndexPrice { get; init; }
    public required decimal FundingRate { get; init; }
    public required int? FundingIntervalHours { get; init; }
    public required DateTimeOffset? NextFundingTime { get; init; }
    public decimal? PredictedNextRate { get; init; }
    public required DateTimeOffset LastSeenOnUtc { get; init; }
    public bool IsActive { get; init; } = true;

    public int NumberOfPaymentsPerDay =>
        FundingIntervalHours is > 0 ? 24 / FundingIntervalHours.Value : 3;

    public decimal Apr => FundingIntervalHours is > 0
        ? FundingRate * 100m * (365m * 24m / FundingIntervalHours.Value)
        : 0m;

    public decimal Deviation =>
        IndexPrice > 0 ? (MarkPrice / IndexPrice - 1m) * 100m : 0m;
}
