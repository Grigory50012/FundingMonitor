using FundingMonitor.Core.Enums;

namespace FundingMonitor.Core.Models;

public class NormalizedFundingRate
{
    // Идентификаторы
    public ExchangeType Exchange { get; set; }
    public string NormalizedSymbol { get; set; } = string.Empty; // "BTC-USDT"
    public string BaseAsset { get; set; } = string.Empty;       // "BTC"
    public string QuoteAsset { get; set; } = string.Empty;      // "USDT"
    public string OriginalSymbol { get; set; } = string.Empty;  // Как было у биржи
    
    // Основные данные
    public decimal FundingRate { get; set; }                    // -0.0001 до +0.0001
    public decimal? PredictedNextRate { get; set; }
    public DateTime NextFundingTime { get; set; }
    public DateTime DataTime { get; set; } = DateTime.UtcNow;
    
    // Дополнительные данные
    public decimal? MarkPrice { get; set; }
    public decimal? IndexPrice { get; set; }
    public decimal? OpenInterest { get; set; }
    public decimal? Volume24h { get; set; }
    public decimal? FundingIntervalHours { get; set; } = 8;
    
    // Статус
    public bool IsActive { get; set; } = true;
    public string? InstrumentType { get; set; }                 // "PERPETUAL", "SWAP"
    
    // Расчетные свойства
    public decimal AnnualizedRate => FundingRate * (365m * 24m / (FundingIntervalHours ?? 8m));
    public bool IsSignificant => Math.Abs(FundingRate) > 0.0003m;
    public string RateDirection => FundingRate >= 0 ? "📈" : "📉";
    
    public override string ToString() => 
        $"{Exchange} {NormalizedSymbol}: {FundingRate:P6} ({NextFundingTime:HH:mm})";
}