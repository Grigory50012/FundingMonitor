namespace FundingMonitor.Core.ValueObjects;

public class Symbol
{
    public string Base { get; }
    public string Quote { get; }
    
    public Symbol(string normalizedSymbol)
    {
        if (string.IsNullOrWhiteSpace(normalizedSymbol))
            throw new ArgumentException("Symbol cannot be empty");
        
        if (normalizedSymbol.Contains('-'))
        {
            var parts = normalizedSymbol.Split('-');
            Base = parts[0];
            Quote = parts.Length > 1 ? parts[1] : string.Empty;
        }
        else if (normalizedSymbol.EndsWith("USDT"))
        {
            Base = normalizedSymbol[..^4];
            Quote = "USDT";
        }
        else
        {
            Base = normalizedSymbol;
            Quote = string.Empty;
        }
    }
    
    public override string ToString() => $"{Base}-{Quote}";
}