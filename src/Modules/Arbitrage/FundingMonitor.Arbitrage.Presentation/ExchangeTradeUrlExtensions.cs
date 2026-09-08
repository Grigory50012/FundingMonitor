using FundingMonitor.SharedKernel;

namespace FundingMonitor.Arbitrage.Presentation;

public static class ExchangeTradeUrlExtensions
{
    public static string ToTradeUrl(this ExchangeId exchange, string normalizedSymbol)
    {
        var symbol = normalizedSymbol.Contains('-')
            ? normalizedSymbol
            : $"{normalizedSymbol}-USDT";
        var compactSymbol = symbol.Replace("-", string.Empty);
        var okxSymbol = symbol.EndsWith("-SWAP", StringComparison.OrdinalIgnoreCase)
            ? symbol
            : $"{symbol}-SWAP";

        return exchange.Value switch
        {
            "Binance" => $"https://www.binance.com/en/futures/{compactSymbol}",
            "Bybit" => $"https://www.bybit.com/trade/usdt/{compactSymbol}",
            "OKX" => $"https://www.okx.com/en/trade-swap/{okxSymbol.ToLowerInvariant()}",
            _ => string.Empty
        };
    }
}
