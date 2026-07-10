namespace FundingMonitor.Core.Entities;

public enum ExchangeType
{
    Binance,
    Bybit,
    OKX
}

/// <summary>
/// Extension-методы для ExchangeType
/// </summary>
public static class ExchangeTypeExtensions
{
    private static readonly string[] ValidExchanges = Enum.GetNames<ExchangeType>();

    /// <summary>
    /// Парсит строку с биржами через запятую в список ExchangeType
    /// </summary>
    /// <param name="exchanges">Строка с биржами (например: "Binance,Bybit")</param>
    /// <returns>Список бирж или null, если строка пустая</returns>
    /// <exception cref="ArgumentException">
    /// Выбрасывается при некорректном имени биржи.
    /// </exception>
    public static List<ExchangeType>? ParseExchanges(this string? exchanges)
    {
        if (string.IsNullOrWhiteSpace(exchanges))
            return null;

        return exchanges
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(exchange => exchange.Trim().ParseExchange())
            .ToList();
    }

    /// <summary>
    /// Парсит имя биржи в ExchangeType
    /// </summary>
    /// <param name="exchangeName">Имя биржи</param>
    /// <returns>Значение ExchangeType</returns>
    /// <exception cref="ArgumentException">
    /// Выбрасывается при неизвестном имени биржи.
    /// </exception>
    public static ExchangeType ParseExchange(this string exchangeName)
    {
        if (Enum.TryParse<ExchangeType>(exchangeName, true, out var result))
            return result;

        throw new ArgumentException(
            $"Invalid exchange name: '{exchangeName}'. Valid values: {string.Join(", ", ValidExchanges)}");
    }
}