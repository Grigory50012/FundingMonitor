using FundingMonitor.SharedKernel;

namespace FundingMonitor.HistoricalRates.Contracts;

public sealed record AprPeriodStatsView(
    ExchangeId Exchange,
    string Period,
    int Days,
    decimal Apr,
    decimal TotalFundingRatePercent,
    int PaymentsCount,
    decimal AvgFundingRatePercent,
    decimal StdDev);
