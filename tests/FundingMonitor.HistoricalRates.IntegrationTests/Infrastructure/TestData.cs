using FundingMonitor.HistoricalRates.Application;
using FundingMonitor.ExchangeIntegration.Contracts;
using FundingMonitor.SharedKernel;

namespace FundingMonitor.HistoricalRates.IntegrationTests.Infrastructure;

internal static class TestEvents
{
    public static HistoricalCollectionRequest BtcChanged => new(
        Guid.Parse("30000000-0000-0000-0000-000000000001"),
        ExchangeId.Parse("Binance"),
        Symbol.Create("BTC-USDT"));
}

internal static class TestHistory
{
    public static HistoricalRatePoint BtcPoint => new(
        ExchangeId.Parse("Binance"),
        Symbol.Create("BTC-USDT"),
        0.0001m,
        new DateTimeOffset(2026, 9, 8, 8, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero));
}
