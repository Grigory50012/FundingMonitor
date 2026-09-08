namespace FundingMonitor.HistoricalRates.Presentation.Dtos;

public sealed record AprPeriodStatsDto(
    string Exchange,
    string Period,
    int Days,
    decimal Apr,
    decimal TotalFundingRatePercent,
    int PaymentsCount,
    decimal AvgFundingRatePercent,
    decimal StdDev);
