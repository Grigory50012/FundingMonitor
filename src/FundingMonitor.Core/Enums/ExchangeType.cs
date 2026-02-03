namespace FundingMonitor.Core.Enums;

public enum ExchangeType
{
    Binance,
    Bybit,
    OKX,
}

public static class ExchangeTypeExtensions
{
    public static string GetApiBaseUrl(this ExchangeType exchangeType) => exchangeType switch
    {
        ExchangeType.Binance => "https://fapi.binance.com",
        ExchangeType.Bybit => "https://api.bybit.com",
        ExchangeType.OKX => "https://www.okx.com",
        _ => throw new ArgumentException($"Unknown exchange: {exchangeType}")
    };

    public static int GetRateLimitPerMinute(this ExchangeType exchangeType) => exchangeType switch
    {
        ExchangeType.Binance => 1200,
        ExchangeType.Bybit => 120,
        ExchangeType.OKX => 20,
        _ => 60
    };
}