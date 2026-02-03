using FundingMonitor.Core.Enums;

namespace FundingMonitor.Core.Services;

public class SymbolNormalizer
{
    public static string Normalize(string symbol, ExchangeType exchange)
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
    
    public static (string Base, string Quote) Parse(string symbol, ExchangeType exchange)
    {
        var normalized = Normalize(symbol, exchange);
        
        if (normalized.Contains('-'))
        {
            var parts = normalized.Split('-');
            return parts.Length >= 2 ? (parts[0], parts[1]) : (normalized, string.Empty);
        }
        
        // Автоопределение
        if (symbol.EndsWith("USDT")) return (symbol[..^4], "USDT");
        if (symbol.EndsWith("BUSD")) return (symbol[..^4], "BUSD");
        if (symbol.EndsWith("USD")) return (symbol[..^3], "USD");
        
        return (symbol, string.Empty);
    }
}