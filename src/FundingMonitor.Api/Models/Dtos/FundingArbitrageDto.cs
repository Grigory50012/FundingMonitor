namespace FundingMonitor.Api.Models.Dtos;

/// <summary>
///     DTO для арбитражной возможности по ставке финансирования
/// </summary>
/// <param name="Symbol">Торговая пара (например, BTC-USDT)</param>
/// <param name="ExchangeA">Биржа A</param>
/// <param name="ExchangeB">Биржа B</param>
/// <param name="PriceA">Цена на бирже A</param>
/// <param name="PriceB">Цена на бирже B</param>
/// <param name="PriceSpread">Спред между ценами</param>
/// <param name="PriceSpreadPercent">Спред между ценами в процентах</param>
/// <param name="APRFundingRateA">APR ставки финансирования на бирже A</param>
/// <param name="APRFundingRateB">APR ставки финансирования на бирже B</param>
/// <param name="APRSpread">Разница APR (A - B)</param>
/// <param name="ProfitabilityPercent">Доходность арбитража</param>
/// <param name="PaymentsA">Количество выплат в день на бирже A</param>
/// <param name="PaymentsB">Количество выплат в день на бирже B</param>
/// <param name="ShortExchange">Биржа для шорта (с более высоким APR)</param>
/// <param name="LongExchange">Биржа для лонга (с более низким APR)</param>
public record FundingArbitrageDto(
    string Symbol,
    string ExchangeA,
    string ExchangeB,
    decimal PriceA,
    decimal PriceB,
    decimal PriceSpread,
    decimal PriceSpreadPercent,
    decimal APRFundingRateA,
    decimal APRFundingRateB,
    decimal APRSpread,
    decimal ProfitabilityPercent,
    int PaymentsA,
    int PaymentsB,
    string ShortExchange,
    string LongExchange
);