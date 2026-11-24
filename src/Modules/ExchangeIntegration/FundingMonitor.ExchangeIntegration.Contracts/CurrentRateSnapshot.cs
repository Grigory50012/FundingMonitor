using FundingMonitor.SharedKernel;

namespace FundingMonitor.ExchangeIntegration.Contracts;

public sealed record CurrentRateSnapshot(
    ExchangeId Exchange,
    Symbol Symbol,
    decimal MarkPrice,
    decimal IndexPrice,
    decimal? FundingRate,
    int? FundingIntervalHours,
    DateTimeOffset? NextFundingTime,
    DateTimeOffset ObservedOnUtc,
    decimal? BidPrice = null,
    decimal? AskPrice = null);
