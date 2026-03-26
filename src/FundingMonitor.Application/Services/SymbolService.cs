using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Services;

namespace FundingMonitor.Application.Services;

/// <summary>
///     Единый сервис для операций с торговыми символами бирж
/// </summary>
public class SymbolService : ISymbolService
{
    public (string Base, string Quote) Parse(string symbol, ExchangeType exchange)
    {
        return exchange switch
        {
            ExchangeType.Binance => ParseBinance(symbol),
            ExchangeType.Bybit => ParseBybit(symbol),
            ExchangeType.OKX => ParseOKX(symbol),
            _ => throw new NotSupportedException($"Exchange {exchange} not supported")
        };
    }

    public string ConvertToExchange(string symbol, ExchangeType exchange)
    {
        return exchange switch
        {
            ExchangeType.Binance
                or ExchangeType.Bybit => symbol.Replace("-", ""),
            ExchangeType.OKX => symbol.EndsWith("-SWAP") ? symbol : symbol + "-SWAP",
            _ => throw new NotSupportedException($"Exchange {exchange} not supported")
        };
    }

    public bool IsValidSymbol(string symbol, ExchangeType exchange)
    {
        return exchange switch
        {
            ExchangeType.Binance
                or ExchangeType.Bybit => symbol.EndsWith("USDT"),
            ExchangeType.OKX => symbol.EndsWith("-USDT-SWAP"),
            _ => false
        };
    }

    private static (string Base, string Quote) ParseBinance(string symbol)
    {
        return symbol.EndsWith("USDT")
            ? (symbol[..^4], "USDT")
            : throw new ArgumentException($"Unknown Binance symbol format: {symbol}");
    }

    private static (string Base, string Quote) ParseBybit(string symbol)
    {
        return symbol.EndsWith("USDT")
            ? (symbol[..^4], "USDT")
            : throw new ArgumentException($"Unknown Bybit symbol format: {symbol}");
    }

    private static (string Base, string Quote) ParseOKX(string symbol)
    {
        // OKX формат: BTC-USDT-SWAP
        if (symbol.EndsWith("-SWAP"))
        {
            var parts = symbol[..^5].Split('-');
            if (parts.Length >= 2)
                return (parts[0], parts[1]);
        }

        throw new ArgumentException($"Unknown OKX symbol format: {symbol}");
    }
}