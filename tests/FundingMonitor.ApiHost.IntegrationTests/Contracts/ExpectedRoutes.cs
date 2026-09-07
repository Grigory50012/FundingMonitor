namespace FundingMonitor.ApiHost.IntegrationTests.Contracts;

internal static class ExpectedRoutes
{
    internal static readonly string[] Paths =
    [
        "/api/v1/FundingRates",
        "/api/v1/History?symbol=BTC-USDT",
        "/api/v1/History/apr-stats?symbol=BTC-USDT",
        "/api/v1/Arbitrage",
        "/api/v1/exchanges/health",
        "/health",
        "/openapi/v1.json"
    ];
}