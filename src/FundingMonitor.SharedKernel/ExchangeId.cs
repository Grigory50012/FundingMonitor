namespace FundingMonitor.SharedKernel;

public readonly record struct ExchangeId
{
    private static readonly HashSet<string> Known =
        new(StringComparer.OrdinalIgnoreCase) { "Binance", "Bybit", "OKX" };

    private ExchangeId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ExchangeId Parse(string value)
    {
        var canonical = Known.FirstOrDefault(item => item.Equals(value, StringComparison.OrdinalIgnoreCase));
        return canonical is null
            ? throw new ArgumentException($"Unknown exchange '{value}'.", nameof(value))
            : new ExchangeId(canonical);
    }

    public override string ToString()
    {
        return Value;
    }
}