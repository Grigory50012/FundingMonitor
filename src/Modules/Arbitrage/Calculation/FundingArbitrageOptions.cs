namespace FundingMonitor.Arbitrage.Calculation;

internal sealed class FundingArbitrageOptions
{
    public const string SectionName = "FundingArbitrage";

    public int MaxRateAgeMinutes { get; init; } = 5;
}
