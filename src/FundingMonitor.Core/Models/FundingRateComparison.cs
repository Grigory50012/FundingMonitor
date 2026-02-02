namespace FundingMonitor.Core.Models;

// Модель для сравнения ставок между биржами
public class FundingRateComparison
{
    public string Symbol { get; set; } = string.Empty;
    public FundingRateInfo BinanceRate { get; set; } = null!;
    public FundingRateInfo BybitRate { get; set; } = null!;
    public decimal Difference => Math.Abs(BinanceRate.Rate - BybitRate.Rate);
    public string SuggestedAction => GetSuggestedAction();
    public decimal PotentialProfit => CalculatePotentialProfit();
    
    private string GetSuggestedAction()
    {
        if (BinanceRate.Rate < 0 && BybitRate.Rate > 0)
            return "LONG Binance, SHORT Bybit";
        else if (BinanceRate.Rate > 0 && BybitRate.Rate < 0)
            return "SHORT Binance, LONG Bybit";
        else if (BinanceRate.Rate > BybitRate.Rate)
            return "Consider SHORT Binance, LONG Bybit";
        else
            return "Consider LONG Binance, SHORT Bybit";
    }
    
    private decimal CalculatePotentialProfit()
    {
        // Базовая формула: разница ставок * 3 выплаты в день * 365 дней
        var dailyProfit = Difference * 3;
        var annualProfit = dailyProfit * 365;
        return annualProfit * 100; // В процентах
    }
}