namespace FundingMonitor.Core.Configuration;

public class ExchangeOptions
{
    public const string BinanceSection = "Exchanges:Binance";
    public const string BybitSection = "Exchanges:Bybit";
    
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int RateLimitPerMinute { get; set; }
}