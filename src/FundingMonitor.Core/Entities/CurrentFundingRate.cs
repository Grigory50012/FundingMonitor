namespace FundingMonitor.Core.Entities;

public record CurrentFundingRate
{
    // Основные данные
    public required ExchangeType Exchange { get; init; } // Биржа
    public required string NormalizedSymbol { get; init; } = string.Empty; // Символ, например "BTC-USDT"
    public required decimal MarkPrice { get; init; } // Расчётная цена
    public required decimal IndexPrice { get; init; } // Индексная цена
    public required decimal FundingRate { get; init; } // Ставка финансирования
    public required int? FundingIntervalHours { get; init; } = 8; // Период выплат
    public required DateTime? NextFundingTime { get; init; } // Время выплаты
    public required DateTime LastCheck { get; init; } = DateTime.UtcNow; // Последняя проверка

    // Опциональные данные
    public decimal? PredictedNextRate { get; init; }

    // Дополнительные данные
    public bool IsActive { get; init; } = true; // Статус
    public required string BaseAsset { get; init; } = string.Empty; // "BTC"
    public required string QuoteAsset { get; init; } = string.Empty; // "USDT"

    // Расчетные свойства
    public int NumberOfPaymentsPerDay => FundingIntervalHours is > 0 ? 24 / FundingIntervalHours.Value : 3;

    public decimal APR => FundingIntervalHours is > 0
        ? FundingRate * 100m * (365m * 24m / FundingIntervalHours.Value)
        : 0m;

    public decimal Deviation => IndexPrice > 0 ? (MarkPrice / IndexPrice - 1m) * 100m : 0m;
}