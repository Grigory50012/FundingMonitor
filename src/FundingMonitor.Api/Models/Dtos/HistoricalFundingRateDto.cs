namespace FundingMonitor.Api.Models.Dtos;

/// <summary>
///     DTO для исторической ставки финансирования
/// </summary>
/// <param name="Exchange">Название биржи</param>
/// <param name="Symbol">Торговая пара (например, BTC-USDT)</param>
/// <param name="FundingRate">Ставка финансирования</param>
/// <param name="FundingTime">Время выплаты</param>
public record HistoricalFundingRateDto(
    string Exchange,
    string Symbol,
    decimal FundingRate,
    DateTime FundingTime
);