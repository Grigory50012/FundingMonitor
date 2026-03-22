namespace FundingMonitor.Api.Models.Dtos;

/// <summary>
///     DTO для текущей ставки финансирования
/// </summary>
/// <param name="Exchange">Название биржи</param>
/// <param name="Symbol">Торговая пара (например, BTC-USDT)</param>
/// <param name="MarkPrice">Расчётная цена</param>
/// <param name="FundingRate">Ставка финансирования</param>
/// <param name="APR">Годовая процентная ставка</param>
/// <param name="NumberOfPaymentsPerDay">Количество выплат в день</param>
/// <param name="NextFundingTime">Время следующей выплаты</param>
public record FundingRateDto(
    string Exchange,
    string Symbol,
    decimal MarkPrice,
    decimal FundingRate,
    decimal APR,
    int NumberOfPaymentsPerDay,
    DateTime? NextFundingTime
);