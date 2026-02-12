using System.Collections.Concurrent;
using FundingMonitor.Application.Interfaces.Services;
using FundingMonitor.Core.Entities;

namespace FundingMonitor.Application.Services;

public class SymbolNormalizer : ISymbolNormalizer
{
    private readonly ConcurrentDictionary<string, string> _normalizeCache = new();
    private readonly ConcurrentDictionary<string, (string Base, string Quote)> _parseCache = new();

    public string Normalize(string symbol, ExchangeType exchange)
    {
        var key = $"{exchange}:{symbol}";
        return _normalizeCache.GetOrAdd(key, _ => NormalizeInternal(symbol, exchange));
    }

    public (string Base, string Quote) Parse(string symbol, ExchangeType exchange)
    {
        var key = $"{exchange}:{symbol}";
        return _parseCache.GetOrAdd(key, _ => ParseInternal(symbol, exchange));
    }

    private static string NormalizeInternal(string symbol, ExchangeType exchange)
    {
        symbol = symbol.ToUpperInvariant();

        return exchange switch
        {
            ExchangeType.Binance => symbol.EndsWith("USDT") ? $"{symbol[..^4]}-USDT" : symbol,
            ExchangeType.Bybit => symbol == "XBTUSDT" ? "BTC-USDT" :
                symbol.EndsWith("USDT") ? $"{symbol[..^4]}-USDT" : symbol,
            ExchangeType.OKX => symbol.EndsWith("-SWAP") ? symbol[..^5] : symbol,
            _ => symbol.Contains('-') ? symbol :
                symbol.EndsWith("USDT") ? $"{symbol[..^4]}-USDT" : symbol
        };
    }

    private (string Base, string Quote) ParseInternal(string symbol, ExchangeType exchange)
    {
        var normalized = Normalize(symbol, exchange);

        if (normalized.Contains('-'))
        {
            var parts = normalized.Split('-');
            return parts.Length >= 2 ? (parts[0], parts[1]) : (normalized, string.Empty);
        }

        if (symbol.EndsWith("USDT")) return (symbol[..^4], "USDT");
        if (symbol.EndsWith("BUSD")) return (symbol[..^4], "BUSD");
        if (symbol.EndsWith("USD")) return (symbol[..^3], "USD");

        return (symbol, string.Empty);
    }
}