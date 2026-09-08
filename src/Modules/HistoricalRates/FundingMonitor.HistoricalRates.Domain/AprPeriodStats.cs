using FundingMonitor.SharedKernel;

namespace FundingMonitor.HistoricalRates.Domain;

public sealed record AprPeriodStats(
    ExchangeId Exchange,
    string Period,
    int Days,
    decimal Apr,
    decimal TotalFundingRatePercent,
    int PaymentsCount,
    decimal AvgFundingRatePercent,
    decimal StdDev);
