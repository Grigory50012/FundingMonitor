namespace FundingMonitor.Core.Configuration;

public class FundingArbitrageOptions
{
    public const string SectionName = "FundingArbitrage";

    public decimal MinSpreadPercent { get; init; } = 0.01m;
}