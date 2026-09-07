using FundingMonitor.SharedKernel;

namespace FundingMonitor.ExchangeIntegration.Domain;

public sealed class SymbolNormalizer
{
    public Symbol Normalize(string exchangeSymbol, ExchangeId exchange)
    {
        var value = exchange.Value switch
        {
            "Binance" or "Bybit" when exchangeSymbol.EndsWith("USDT", StringComparison.OrdinalIgnoreCase) =>
                $"{exchangeSymbol[..^4]}-USDT",
            "OKX" when exchangeSymbol.EndsWith("-USDT-SWAP", StringComparison.OrdinalIgnoreCase) =>
                exchangeSymbol[..^5],
            _ => throw new ArgumentException(
                $"Unknown {exchange} symbol format: {exchangeSymbol}",
                nameof(exchangeSymbol))
        };

        return Symbol.Create(value);
    }

    public string ToExchangeSymbol(Symbol symbol, ExchangeId exchange) =>
        exchange.Value switch
        {
            "Binance" or "Bybit" => symbol.Value.Replace("-", string.Empty, StringComparison.Ordinal),
            "OKX" => $"{symbol.Value}-SWAP",
            _ => throw new ArgumentException($"Unsupported exchange '{exchange}'.", nameof(exchange))
        };

    public bool IsValid(string exchangeSymbol, ExchangeId exchange) =>
        exchange.Value switch
        {
            "Binance" or "Bybit" => exchangeSymbol.EndsWith("USDT", StringComparison.OrdinalIgnoreCase),
            "OKX" => exchangeSymbol.EndsWith("-USDT-SWAP", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
}
