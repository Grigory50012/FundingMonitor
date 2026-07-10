using FundingMonitor.Core.Entities;

namespace FundingMonitor.Api.Extensions;

public static class ExchangeTradeUrlExtensions
{
    public static string ToTradeUrl(this ExchangeType exchange, string normalizedSymbol)
    {
        var symbol = normalizedSymbol.Contains('-')
            ? normalizedSymbol
            : $"{normalizedSymbol}-USDT";
        var compactSymbol = symbol.Replace("-", "");
        var okxSymbol = symbol.EndsWith("-SWAP", StringComparison.OrdinalIgnoreCase)
            ? symbol
            : $"{symbol}-SWAP";

        return exchange switch
        {
            ExchangeType.Binance => $"https://www.binance.com/en/futures/{compactSymbol}",
            ExchangeType.Bybit => $"https://www.bybit.com/trade/usdt/{compactSymbol}",
            ExchangeType.OKX => $"https://www.okx.com/en/trade-swap/{okxSymbol.ToLowerInvariant()}",
            _ => string.Empty
        };
    }
}