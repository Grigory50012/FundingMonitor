namespace FundingMonitor.Api.Models.Dtos;

/// <summary>
///     DTO для статистики APR по периодам
/// </summary>
/// <param name="Exchange">Название биржи</param>
/// <param name="Period">Название периода (например, "7 дней")</param>
/// <param name="Days">Количество дней в периоде</param>
/// <param name="Apr">Годовая процентная ставка (APR) в процентах</param>
/// <param name="TotalFundingRatePercent">Суммарная ставка финансирования за период в процентах</param>
/// <param name="PaymentsCount">Количество выплат за период</param>
/// <param name="AvgFundingRatePercent">Средняя ставка за выплату в процентах</param>
/// <param name="StdDev">Среднеквадратическое отклонение ставки за период в процентах</param>
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