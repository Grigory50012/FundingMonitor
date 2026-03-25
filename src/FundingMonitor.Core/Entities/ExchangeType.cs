using System.Collections.Concurrent;

namespace FundingMonitor.Core.Entities;

public enum ExchangeType
{
    Binance,
    Bybit,
    OKX
}

/// <summary>
///     Extension-методы для ExchangeType
/// </summary>
public static class ExchangeTypeExtensions
{
    private static readonly string[] ValidExchanges = Enum.GetNames<ExchangeType>();
    private static readonly ConcurrentDictionary<string, ExchangeType> ExchangeCache = new();

    /// <summary>
    ///     Парсит строку с биржами через запятую в список ExchangeType
    /// </summary>
    /// <param name="exchanges">Строка с биржами (например: "Binance,Bybit")</param>
    /// <returns>Список бирж или null, если строка пустая</returns>
    /// <exception cref="ArgumentException">Выбрасывается при некорректном имени биржи</exception>
    public static List<ExchangeType>? ParseExchanges(this string? exchanges)
    {
        if (string.IsNullOrWhiteSpace(exchanges))
            return null;

        return exchanges.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(e => ParseSingle(e.Trim()))
            .ToList();
    }

    /// <summary>
    ///     Парсит одиночное имя биржи в ExchangeType с кэшированием
    /// </summary>
    /// <param name="exchangeName">Имя биржи</param>
    /// <returns>Значение ExchangeType</returns>
    /// <exception cref="InvalidOperationException">Выбрасывается при неизвестном имени биржи</exception>
    public static ExchangeType ParseExchange(this string exchangeName)
    {
        return ExchangeCache.GetOrAdd(exchangeName, name =>
        {
            if (Enum.TryParse<ExchangeType>(name, true, out var result))
                return result;

            throw new InvalidOperationException(
                $"Unknown exchange: {name}. Valid values: {string.Join(", ", ValidExchanges)}");
        });
    }

    private static ExchangeType ParseSingle(string exchangeName)
    {
        if (Enum.TryParse<ExchangeType>(exchangeName, true, out var result))
            return result;

        throw new ArgumentException(
            $"Invalid exchange name: '{exchangeName}'. Valid values: {string.Join(", ", ValidExchanges)}");
    }
}