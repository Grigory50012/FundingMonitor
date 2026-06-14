namespace FundingMonitor.Api.Models.Dtos;

/// <summary>
///     DTO для статистики APR по периодам
/// </summary>
/// <remarks>
///     Агрегированная статистика по историческим ставкам финансирования за различные временные окна.
///     Полезна для оценки доходности арбитражных стратегий и волатильности ставок.
///     
///     Периоды: 1 день, 7 дней, 14 дней, 30 дней, 90 дней.
///     APR рассчитывается как взвешенное среднее по всем выплатам в периоде с годовкой.
///     
///     Пример ответа:
///     [
///       {
///         "exchange": "Binance",
///         "period": "7 дней",
///         "days": 7,
///         "apr": 12.45,
///         "totalFundingRatePercent": 0.238,
///         "paymentsCount": 21,
///         "avgFundingRatePercent": 0.0113,
///         "stdDev": 0.0042
///       }
///     ]
/// </remarks>
/// <param name="Exchange">Название биржи (Binance, Bybit, OKX)</param>
/// <param name="Period">Человекочитаемое название периода (например, "7 дней", "30 дней")</param>
/// <param name="Days">Количество календарных дней в периоде</param>
/// <param name="Apr">Годовая процентная ставка (APR) в процентах. Экстраполированная годовая доходность на основе средней ставки за период</param>
/// <param name="TotalFundingRatePercent">Суммарная ставка финансирования за весь период в процентах (сумма всех выплат)</param>
/// <param name="PaymentsCount">Общее количество выплат финансирования за период</param>
/// <param name="AvgFundingRatePercent">Средняя ставка за одну выплату в процентах</param>
/// <param name="StdDev">Стандартное отклонение ставки финансирования за период (мера волатильности)</param>
public record AprPeriodStatsDto(
    string Exchange,
    string Period,
    int Days,
    decimal Apr,
    decimal TotalFundingRatePercent,
    int PaymentsCount,
    decimal AvgFundingRatePercent,
    decimal StdDev
);