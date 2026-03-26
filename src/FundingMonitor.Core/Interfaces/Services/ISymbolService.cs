using FundingMonitor.Core.Entities;

namespace FundingMonitor.Core.Interfaces.Services;

/// <summary>
///     Сервис для операций с торговыми символами бирж
/// </summary>
public interface ISymbolService
{
    /// <summary>
    ///     Парсит символ биржи на базовую и котируемую валюты
    /// </summary>
    /// <param name="symbol">Символ в формате биржи</param>
    /// <param name="exchange">Тип биржи</param>
    /// <returns>Кортеж (базовая валюта, котируемая валюта)</returns>
    (string Base, string Quote) Parse(string symbol, ExchangeType exchange);

    /// <summary>
    ///     Конвертирует нормализованный символ в формат конкретной биржи
    /// </summary>
    /// <param name="symbol">Символ в нормализованном формате (например, BTC-USDT)</param>
    /// <param name="exchange">Тип биржи</param>
    /// <returns>Символ в формате биржи</returns>
    string ConvertToExchange(string symbol, ExchangeType exchange);

    /// <summary>
    ///     Проверяет валидность символа для конкретной биржи
    /// </summary>
    /// <param name="symbol">Символ в формате биржи</param>
    /// <param name="exchange">Тип биржи</param>
    /// <returns>True если символ валиден</returns>
    bool IsValidSymbol(string symbol, ExchangeType exchange);
}