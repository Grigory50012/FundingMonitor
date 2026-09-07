namespace FundingMonitor.SharedKernel;

public readonly record struct Symbol
{
    private Symbol(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Symbol Create(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length < 3 || !normalized.Contains("-", StringComparison.Ordinal))
            throw new ArgumentException($"Invalid normalized symbol '{value}'.", nameof(value));

        return new Symbol(normalized);
    }

    public override string ToString()
    {
        return Value;
    }
}