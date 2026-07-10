namespace FundingMonitor.Api.Models.Dtos;

/// <summary>
///     DTO для арбитражной возможности по ставке финансирования (Funding Rate Arbitrage)
/// </summary>
/// <remarks>
///     Арбитраж на ставке финансирования: открытие встречных позиций (Short на бирже с высоким APR,
///     Long на бирже с низким/отрицательным APR) для получения разницы ставок без направленного риска.
///     
///     Стратегия:
///     1. Short на бирже с более высоким funding rate (получаем выплаты)
///     2. Long на бирже с более низким funding rate (платим меньшие выплаты)
///     3. Профит = Разница APR между биржами (за вычетом комиссий и спреда цен)
///     
///     Пример ответа:
///     [
///       {
///         "symbol": "BTC-USDT",
///         "exchangeA": "Binance",
///         "exchangeB": "Bybit",
///         "priceA": 65000.50,
///         "priceB": 64980.00,
///         "priceSpread": 20.50,
///         "priceSpreadPercent": 0.0315,
///         "fundingRateA": 0.000150,
///         "fundingRateB": 0.000050,
///         "fundingRateSpread": 0.000100,
///         "paymentsA": 3,
///         "paymentsB": 3,
///         "shortExchange": "Binance",
///         "longExchange": "Bybit",
///         "exchangeAUrl": "https://www.binance.com/en/futures/BTCUSDT",
///         "exchangeBUrl": "https://www.bybit.com/trade/usdt/BTCUSDT"
///       }
///     ]
/// </remarks>
/// <param name="Symbol">Торговая пара (например, BTC-USDT)</param>
/// <param name="ExchangeA">Первая биржа в паре</param>
/// <param name="ExchangeB">Вторая биржа в паре</param>
/// <param name="PriceA">Mark Price на бирже A</param>
/// <param name="PriceB">Mark Price на бирже B</param>
/// <param name="PriceSpread">Абсолютный спред цен между биржами (PriceA - PriceB)</param>
/// <param name="PriceSpreadPercent">Спред цен в процентах от цены биржа B</param>
/// <param name="FundingRateA">Ставка финансирования на бирже A</param>
/// <param name="FundingRateB">Ставка финансирования на бирже B</param>
/// <param name="FundingRateSpread">Разница ставок финансирования (FundingRateA - FundingRateB)</param>
/// <param name="PaymentsA">Количество выплат в день на бирже A</param>
/// <param name="PaymentsB">Количество выплат в день на бирже B</param>
/// <param name="ShortExchange">Биржа для открытия Short (где APR выше)</param>
/// <param name="LongExchange">Биржа для открытия Long (где APR ниже)</param>
/// <param name="ExchangeAUrl">External trading page URL for exchange A</param>
/// <param name="ExchangeBUrl">External trading page URL for exchange B</param>
public record FundingArbitrageDto(
    string Symbol,
    string ExchangeA,
    string ExchangeB,
    decimal PriceA,
    decimal PriceB,
    decimal PriceSpread,
    decimal PriceSpreadPercent,
    decimal FundingRateA,
    decimal FundingRateB,
    decimal FundingRateSpread,
    int PaymentsA,
    int PaymentsB,
    string ShortExchange,
    string LongExchange,
    string ExchangeAUrl,
    string ExchangeBUrl
);