namespace FundingMonitor.Core.Entities;

public class ArbitrageOpportunity
{
    public string Symbol { get; set; } = string.Empty;
    public List<CurrentFundingRate> Rates { get; set; } = new();

    // Расчетные свойства
    public decimal MaxDifference => Rates.Count < 2 ? 0 : Rates.Max(r => r.FundingRate) - Rates.Min(r => r.FundingRate);

    public ExchangeType? BestExchange =>
        Rates.OrderBy(r => r.FundingRate).FirstOrDefault()?.Exchange;

    public ExchangeType? WorstExchange =>
        Rates.OrderByDescending(r => r.FundingRate).FirstOrDefault()?.Exchange;

    public decimal AnnualYieldPercent => MaxDifference * (365m * 24m / 8m) * 100;

    public string Action => Rates.Count < 2
        ? "Need 2+ exchanges"
        : $"LONG {BestExchange} ({Rates.First(r => r.Exchange == BestExchange)?.FundingRate:P4}) / " +
          $"SHORT {WorstExchange} ({Rates.First(r => r.Exchange == WorstExchange)?.FundingRate:P4})";

    public bool IsProfitable => MaxDifference > 0.0001m; // > 0.01% разницы
}