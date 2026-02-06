using FundingMonitor.Core.Enums;

namespace FundingMonitor.Core.Models;

public class NormalizedFundingRate
{
    // Основные данные
    public ExchangeType Exchange { get; set; } // Биржа
    public string NormalizedSymbol { get; set; } = string.Empty; // "BTC-USDT"
    public decimal MarkPrice { get; set; } // Расчётная цена
    public decimal IndexPrice { get; set; } // Индексная цена
    public decimal FundingRate { get; set; }  // Ставка
    public int? FundingIntervalHours { get; set; } = 8; // Период выплат
    public DateTime NextFundingTime { get; set; } // Время выплаты
    public DateTime LastCheck { get; set; } = DateTime.UtcNow; // Последняя проверка

    // Опциональные данные
    public decimal? PredictedNextRate { get; set; }

    // Дополнительные данные
    public bool IsActive { get; set; } = true; // Статус
    public string BaseAsset { get; set; } = string.Empty; // "BTC"
    public string QuoteAsset { get; set; } = string.Empty; // "USDT"
    
    // Расчетные свойства
    public int NumberOfPaymentsPerDay => 24 / FundingIntervalHours ?? 8;
    public decimal APR => FundingRate * 100m * (365m * 24m / (FundingIntervalHours ?? 8m));
    public decimal Deviation => (MarkPrice / IndexPrice - 1m) * 100m;
    
    public override string ToString() => 
        $"{Exchange} {NormalizedSymbol}: {FundingRate:P5} {NumberOfPaymentsPerDay} {NextFundingTime:HH:mm} {APR:P2}%";
}