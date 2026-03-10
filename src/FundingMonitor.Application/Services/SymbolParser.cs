using FundingMonitor.Core.Entities;
using FundingMonitor.Core.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;

namespace FundingMonitor.Application.Services;

public class SymbolParser : ISymbolParser
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private static readonly IReadOnlyDictionary<ExchangeType, Func<string, (string Base, string Quote)>> Parsers =
        new Dictionary<ExchangeType, Func<string, (string Base, string Quote)>>
        {
            [ExchangeType.Binance] = ParseBinance,
            [ExchangeType.Bybit] = ParseBybit
        };

    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public (string Base, string Quote) Parse(string symbol, ExchangeType exchange)
    {
        var cacheKey = $"{exchange}_{symbol}";

        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            if (!Parsers.TryGetValue(exchange, out var parser))
                throw new NotSupportedException($"Exchange {exchange} not supported");

            return parser(symbol);
        })!;
    }

    private static (string Base, string Quote) ParseBinance(string symbol)
    {
        return symbol.EndsWith("USDT") ? (symbol[..^4], "USDT") :
            symbol.EndsWith("BUSD") ? (symbol[..^4], "BUSD") :
            symbol.EndsWith("USDC") ? (symbol[..^4], "USDC") :
            throw new ArgumentException($"Unknown Binance symbol format: {symbol}");
    }

    private static (string Base, string Quote) ParseBybit(string symbol)
    {
        return symbol.EndsWith("USDT") ? (symbol[..^4], "USDT") :
            symbol.EndsWith("USDC") ? (symbol[..^4], "USDC") :
            throw new ArgumentException($"Unknown Bybit symbol format: {symbol}");
    }
}