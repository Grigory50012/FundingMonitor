namespace FundingMonitor.Arbitrage.Application;

public sealed class FundingArbitrageOptions
{
    public const string SectionName = "FundingArbitrage";

    public decimal MinSpreadPercent { get; init; } = 0.01m;
    public int MaxRateAgeMinutes { get; init; } = 5;
}
