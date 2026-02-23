namespace FundingMonitor.Core.Configuration;

public class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public ExchangeRateLimit Binance { get; set; } = new();
    public ExchangeRateLimit Bybit { get; set; } = new();
}