using FundingMonitor.SharedKernel;

namespace FundingMonitor.CurrentRates.Contracts;

public sealed record CurrentRateView(
    ExchangeId Exchange,
    Symbol Symbol,
    decimal MarkPrice,
    decimal IndexPrice,
    decimal? FundingRate,
    int? FundingIntervalHours,
    DateTimeOffset? NextFundingTime,
    DateTimeOffset LastSeenOnUtc,
    bool IsActive,
    decimal Apr,
    decimal Deviation,
    decimal? BidPrice = null,
    decimal? AskPrice = null);
