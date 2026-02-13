namespace FundingMonitor.Core.Configuration;

public class ExchangeOptions
{
    public const string BinanceSection = "Exchanges:Binance";
    public const string BybitSection = "Exchanges:Bybit";

    public int TimeoutSeconds { get; set; } = 15;
}