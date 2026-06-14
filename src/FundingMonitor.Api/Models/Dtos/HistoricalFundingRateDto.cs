namespace FundingMonitor.Api.Models.Dtos;

/// <summary>
///     DTO для исторической ставки финансирования
/// </summary>
/// <remarks>
///     Исторические данные собираются фоновыми сервисами при изменении ставки или появлении новых символов.
///     Данные хранятся в PostgreSQL с первичным ключом (Exchange, Symbol, FundingTime).
///     
///     Пример ответа:
///     [
///       {
///         "exchange": "Binance",
///         "symbol": "BTC-USDT",
///         "fundingRate": 0.000125,
///         "fundingTime": "2026-06-14T08:00:00Z"
///       }
///     ]
/// </remarks>
/// <param name="Exchange">Название биржи (Binance, Bybit, OKX)</param>
/// <param name="Symbol">Торговая пара в формате BASE-QUOTE (например, BTC-USDT)</param>
/// <param name="FundingRate">Ставка финансирования за период (например, 0.000125 = 0.0125%)</param>
/// <param name="FundingTime">Время начисления/выплаты финансирования в UTC (ISO 8601)</param>
public record HistoricalFundingRateDto(
    string Exchange,
    string Symbol,
    decimal FundingRate,
    DateTime FundingTime
);