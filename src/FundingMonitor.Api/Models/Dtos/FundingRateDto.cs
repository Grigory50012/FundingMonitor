namespace FundingMonitor.Api.Models.Dtos;

/// <summary>
///     DTO для текущей ставки финансирования (funding rate)
/// </summary>
/// <remarks>
///     Ставка финансирования — это периодические выплаты между держателями длинных и коротких позиций
///     на бессрочных фьючерсах. Положительная ставка означает, что лонги платят шортам, отрицательная — наоборот.
///     
///     Пример ответа:
///     [
///       {
///         "exchange": "Binance",
///         "symbol": "BTC-USDT",
///         "markPrice": 65000.50,
///         "fundingRate": 0.0001,
///         "apr": 10.95,
///         "numberOfPaymentsPerDay": 3,
///         "nextFundingTime": "2026-06-14T16:00:00Z"
///       }
///     ]
/// </remarks>
/// <param name="Exchange">Название биржи (Binance, Bybit, OKX)</param>
/// <param name="Symbol">Торговая пара в формате BASE-QUOTE (например, BTC-USDT, ETH-USDT)</param>
/// <param name="MarkPrice">Mark Price (индексная цена) — справочная цена для расчёта PnL и ликвидаций</param>
/// <param name="FundingRate">Ставка финансирования за период (например, 0.0001 = 0.01%)</param>
/// <param name="APR">Годовая процентная ставка (APR) в процентах. Расчёт: FundingRate * 365 * (24 / FundingIntervalHours) * 100</param>
/// <param name="NumberOfPaymentsPerDay">Количество выплат в день (обычно 3 для 8-часовых интервалов)</param>
/// <param name="NextFundingTime">Время следующей выплаты финансирования в UTC (ISO 8601)</param>
public record FundingRateDto(
    string Exchange,
    string Symbol,
    decimal MarkPrice,
    decimal FundingRate,
    decimal APR,
    int NumberOfPaymentsPerDay,
    DateTime? NextFundingTime
);