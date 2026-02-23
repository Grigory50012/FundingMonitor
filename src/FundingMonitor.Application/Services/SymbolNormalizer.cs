using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Services;

namespace FundingMonitor.Application.Services;

public class SymbolNormalizer : ISymbolNormalizer
{
    public string Normalize(string symbol, ExchangeType exchange)
    {
        var (baseAsset, quoteAsset) = Parse(symbol, exchange);
        return $"{baseAsset}-{quoteAsset}";
    }

    public (string Base, string Quote) Parse(string symbol, ExchangeType exchange)
    {
        return exchange switch
        {
            ExchangeType.Binance => ParseBinance(symbol),
            ExchangeType.Bybit => ParseBybit(symbol),
            _ => throw new NotSupportedException($"Exchange {exchange} not supported")
        };
    }

    private (string Base, string Quote) ParseBinance(string symbol)
    {
        if (symbol.EndsWith("USDT"))
            return (symbol[..^4], "USDT");
        if (symbol.EndsWith("BUSD"))
            return (symbol[..^4], "BUSD");
        if (symbol.EndsWith("USDC"))
            return (symbol[..^4], "USDC");

        throw new ArgumentException($"Unknown Binance symbol format: {symbol}");
    }

    private (string Base, string Quote) ParseBybit(string symbol)
    {
        if (symbol.EndsWith("USDT"))
            return (symbol[..^4], "USDT");
        if (symbol.EndsWith("USDC"))
            return (symbol[..^4], "USDC");

        throw new ArgumentException($"Unknown Bybit symbol format: {symbol}");
    }
}