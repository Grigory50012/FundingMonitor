using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;

namespace FundingMonitor.Application.Services;

public class SymbolNormalizer : ISymbolNormalizer
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);

    public (string Base, string Quote) Parse(string symbol, ExchangeType exchange)
    {
        var cacheKey = $"{exchange}_{symbol}";

        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;

            return exchange switch
            {
                ExchangeType.Binance => ParseBinance(symbol),
                ExchangeType.Bybit => ParseBybit(symbol),
                _ => throw new NotSupportedException($"Exchange {exchange} not supported")
            };
        });
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